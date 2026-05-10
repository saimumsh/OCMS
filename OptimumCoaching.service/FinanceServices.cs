using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class FeeService : IFeeService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public FeeService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public async Task<(bool Success, string Message, StudentFeeAccount? Account)> EnsureAccountAsync(
            Guid studentId, Guid batchId, Guid? actorId)
        {
            if (studentId == Guid.Empty || batchId == Guid.Empty)
                return (false, "Student and batch are required", null);

            var existing = await _db.StudentFeeAccounts.FirstOrDefaultAsync(a =>
                !a.IsDeleted && a.StudentId == studentId && a.BatchId == batchId);
            if (existing != null) return (true, "Account exists", existing);

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return (false, "Batch not found", null);

            var account = new StudentFeeAccount
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                BatchId = batchId,
                FinalFee = batch.CourseFee,
                AmountPaid = 0m,
                DiscountAmount = 0m,
                Status = batch.CourseFee > 0 ? FeeAccountStatus.Unpaid : FeeAccountStatus.PaidInFull,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = actorId
            };
            _db.StudentFeeAccounts.Add(account);
            await _uow.CompleteAsync();
            return (true, "Fee account created", account);
        }

        public Task<StudentFeeAccount?> GetAccountAsync(Guid studentId, Guid batchId) =>
            _db.StudentFeeAccounts
                .Include(a => a.Batch)
                .Include(a => a.Payments.OrderByDescending(p => p.PaidOn))
                .FirstOrDefaultAsync(a => !a.IsDeleted && a.StudentId == studentId && a.BatchId == batchId);

        public Task<IList<StudentFeeAccount>> GetForBatchAsync(Guid batchId) =>
            _db.StudentFeeAccounts
                .Include(a => a.Student)
                .Where(a => !a.IsDeleted && a.BatchId == batchId)
                .OrderBy(a => a.Student.FullName)
                .ToListAsync()
                .ContinueWith(t => (IList<StudentFeeAccount>)t.Result);

        public Task<IList<StudentFeeAccount>> GetForStudentAsync(Guid studentId) =>
            _db.StudentFeeAccounts
                .Include(a => a.Batch)
                .Include(a => a.Payments.OrderByDescending(p => p.PaidOn))
                .Where(a => !a.IsDeleted && a.StudentId == studentId)
                .OrderByDescending(a => a.Created)
                .ToListAsync()
                .ContinueWith(t => (IList<StudentFeeAccount>)t.Result);

        public async Task<(bool Success, string Message, FeePayment? Payment)> RecordPaymentAsync(
            Guid accountId, decimal amount, DateTime paidOn,
            PaymentMethod method, string? receiptNumber, string? note, Guid? recordedBy)
        {
            if (amount <= 0) return (false, "Amount must be greater than zero", null);

            var account = await _db.StudentFeeAccounts
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
            if (account == null) return (false, "Fee account not found", null);

            var newPaid = account.AmountPaid + amount;
            if (newPaid > account.FinalFee + 0.001m)
                return (false, $"Amount exceeds remaining balance of {account.Balance:0.00}", null);

            var payment = new FeePayment
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Amount = amount,
                PaidOn = paidOn == default ? DateTime.UtcNow : paidOn,
                Method = method,
                ReceiptNumber = receiptNumber,
                Note = note,
                RecordedByUserId = recordedBy,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = recordedBy
            };
            _db.FeePayments.Add(payment);

            account.AmountPaid = newPaid;
            account.Status = newPaid >= account.FinalFee
                ? FeeAccountStatus.PaidInFull
                : FeeAccountStatus.PartiallyPaid;
            if (account.Status == FeeAccountStatus.PaidInFull && !account.FullyPaidOn.HasValue)
                account.FullyPaidOn = DateTime.UtcNow;
            account.LastModified = DateTime.UtcNow;
            account.LastModifiedBy = recordedBy;

            await _uow.CompleteAsync();
            return (true, $"Payment of {amount:0.00} recorded", payment);
        }

        public async Task<(bool Success, string Message)> ApplyDiscountAsync(
            Guid accountId, decimal discountAmount, string? reason, Guid? actorId)
        {
            if (discountAmount < 0) return (false, "Discount cannot be negative");

            var account = await _db.StudentFeeAccounts
                .Include(a => a.Batch)
                .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
            if (account == null) return (false, "Fee account not found");

            // Reset to base course fee, then apply the new discount.
            var baseFee = account.Batch.CourseFee;
            if (discountAmount > baseFee) return (false, "Discount cannot exceed the course fee");

            account.DiscountAmount = discountAmount;
            account.DiscountReason = reason;
            account.FinalFee = baseFee - discountAmount;
            // Re-derive status based on already-paid amount.
            if (account.AmountPaid >= account.FinalFee)
                account.Status = FeeAccountStatus.PaidInFull;
            else if (account.AmountPaid > 0)
                account.Status = FeeAccountStatus.PartiallyPaid;
            else
                account.Status = FeeAccountStatus.Unpaid;

            account.LastModified = DateTime.UtcNow;
            account.LastModifiedBy = actorId;

            await _uow.CompleteAsync();
            return (true, "Discount applied");
        }

        public async Task<bool> IsExamAdmitEligibleAsync(Guid studentId, Guid batchId)
        {
            var account = await _db.StudentFeeAccounts.FirstOrDefaultAsync(a =>
                !a.IsDeleted && a.StudentId == studentId && a.BatchId == batchId);
            if (account == null) return false; // no account → block until admin creates one

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return false;

            // Eligible if course is free, fully paid, or minimum enrollment cleared.
            if (batch.CourseFee <= 0) return true;
            if (account.Status == FeeAccountStatus.PaidInFull) return true;
            return account.AmountPaid >= batch.MinimumEnrollment;
        }
    }

    public class SalaryService : ISalaryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public SalaryService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<TeacherSalaryPayment>> GetForTeacherAsync(Guid teacherId) =>
            _db.TeacherSalaryPayments
                .Include(s => s.RecordedByUser)
                .Where(s => !s.IsDeleted && s.TeacherId == teacherId)
                .OrderByDescending(s => s.PeriodMonth)
                .ToListAsync()
                .ContinueWith(t => (IList<TeacherSalaryPayment>)t.Result);

        public Task<IList<TeacherSalaryPayment>> GetAllAsync(int? year = null, int? month = null)
        {
            var q = _db.TeacherSalaryPayments
                .Include(s => s.Teacher)
                .Where(s => !s.IsDeleted);
            if (year.HasValue) q = q.Where(s => s.PeriodMonth.Year == year);
            if (month.HasValue) q = q.Where(s => s.PeriodMonth.Month == month);
            return q.OrderByDescending(s => s.PaidOn).ToListAsync()
                .ContinueWith(t => (IList<TeacherSalaryPayment>)t.Result);
        }

        public async Task<(bool Success, string Message, TeacherSalaryPayment? Payment)> RecordAsync(
            Guid teacherId, DateTime periodMonth, decimal amount, DateTime paidOn,
            PaymentMethod method, string? reference, string? note, Guid? recordedBy)
        {
            if (teacherId == Guid.Empty) return (false, "Teacher is required", null);
            if (amount <= 0) return (false, "Amount must be greater than zero", null);

            var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == teacherId && !t.IsDeleted);
            if (!teacherExists) return (false, "Teacher not found", null);

            // Snap PeriodMonth to the first day of the month for consistency.
            var period = new DateTime(periodMonth.Year, periodMonth.Month, 1);

            var payment = new TeacherSalaryPayment
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                PeriodMonth = period,
                Amount = amount,
                PaidOn = paidOn == default ? DateTime.UtcNow : paidOn,
                Method = method,
                Reference = reference,
                Note = note,
                Status = SalaryPaymentStatus.Paid,
                RecordedByUserId = recordedBy,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = recordedBy
            };
            _db.TeacherSalaryPayments.Add(payment);
            await _uow.CompleteAsync();
            return (true, $"Salary {amount:0.00} recorded", payment);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var existing = await _db.TeacherSalaryPayments.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Salary payment not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Salary payment removed");
        }
    }
}
