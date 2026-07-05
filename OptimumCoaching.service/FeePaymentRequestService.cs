using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class FeePaymentRequestService : IFeePaymentRequestService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;
        private readonly IFeeService _fees;

        public FeePaymentRequestService(
            ApplicationDbContext db, IUnitOfWork uow, IFeeService fees)
        {
            _db = db; _uow = uow; _fees = fees;
        }

        public async Task<(bool Success, string Message, FeePaymentRequest? Request)> SubmitAsync(
            Guid accountId, Guid submittedByUserId,
            decimal amount, PaymentMethod method,
            string? transactionReference, string? receiptImagePath, string? note)
        {
            if (amount <= 0) return (false, "Amount must be greater than zero", null);

            var account = await _db.StudentFeeAccounts
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
            if (account == null) return (false, "Fee account not found", null);

            // Ownership check — the submitting user must own the student row.
            if (account.Student.UserId != submittedByUserId)
                return (false, "You are not the owner of this fee account", null);

            if (amount > account.Balance + 0.001m)
                return (false, $"Amount exceeds remaining balance of {account.Balance:0.00}", null);

            var req = new FeePaymentRequest
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                SubmittedByUserId = submittedByUserId,
                Amount = amount,
                Method = method,
                TransactionReference = transactionReference,
                ReceiptImagePath = receiptImagePath,
                Note = note,
                SubmittedAt = DateTime.UtcNow,
                Status = FeePaymentRequestStatus.Pending,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = submittedByUserId
            };
            _db.FeePaymentRequests.Add(req);
            await _uow.CompleteAsync();
            return (true, "Submitted — Finance will verify shortly", req);
        }

        public Task<IList<FeePaymentRequest>> GetForStudentUserAsync(Guid userId) =>
            _db.FeePaymentRequests
                .Include(r => r.Account).ThenInclude(a => a.Batch)
                .Include(r => r.ReviewedByUser)
                .Where(r => !r.IsDeleted && r.SubmittedByUserId == userId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync()
                .ContinueWith(t => (IList<FeePaymentRequest>)t.Result);

        public Task<IList<FeePaymentRequest>> GetForAccountAsync(Guid accountId) =>
            _db.FeePaymentRequests
                .Include(r => r.SubmittedByUser)
                .Where(r => !r.IsDeleted && r.AccountId == accountId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync()
                .ContinueWith(t => (IList<FeePaymentRequest>)t.Result);

        public Task<IList<FeePaymentRequest>> GetPendingAsync() =>
            _db.FeePaymentRequests
                .Include(r => r.Account).ThenInclude(a => a.Student)
                .Include(r => r.Account).ThenInclude(a => a.Batch)
                .Include(r => r.SubmittedByUser)
                .Where(r => !r.IsDeleted && r.Status == FeePaymentRequestStatus.Pending)
                .OrderBy(r => r.SubmittedAt) // oldest first — process FIFO
                .ToListAsync()
                .ContinueWith(t => (IList<FeePaymentRequest>)t.Result);

        public Task<FeePaymentRequest?> GetByIdAsync(Guid id) =>
            _db.FeePaymentRequests
                .Include(r => r.Account).ThenInclude(a => a.Student)
                .Include(r => r.Account).ThenInclude(a => a.Batch)
                .Include(r => r.SubmittedByUser)
                .Include(r => r.ReviewedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

        public Task<int> GetPendingCountAsync() =>
            _db.FeePaymentRequests.CountAsync(r =>
                !r.IsDeleted && r.Status == FeePaymentRequestStatus.Pending);

        public async Task<(bool Success, string Message)> ApproveAsync(Guid requestId, Guid? reviewerUserId)
        {
            var req = await _db.FeePaymentRequests
                .Include(r => r.Account)
                .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);
            if (req == null) return (false, "Request not found");
            if (req.Status != FeePaymentRequestStatus.Pending)
                return (false, $"Request is already {req.Status}");

            // Hand-off to the existing FeeService so receipt #, balance, status,
            // FullyPaidOn all update consistently.
            var note = string.IsNullOrWhiteSpace(req.Note)
                ? $"Self-submitted by student — Trx: {req.TransactionReference ?? "—"}"
                : $"{req.Note} (Trx: {req.TransactionReference ?? "—"})";

            var (ok, msg, payment) = await _fees.RecordPaymentAsync(
                req.AccountId, req.Amount, req.SubmittedAt, req.Method,
                receiptNumber: null, // auto-assign from PaymentSettings
                note: note,
                recordedBy: reviewerUserId);

            if (!ok || payment == null) return (false, msg);

            req.Status = FeePaymentRequestStatus.Approved;
            req.ReviewedByUserId = reviewerUserId;
            req.ReviewedAt = DateTime.UtcNow;
            req.LinkedPaymentId = payment.Id;
            req.LastModified = DateTime.UtcNow;
            req.LastModifiedBy = reviewerUserId;
            await _uow.CompleteAsync();
            return (true, "Payment approved and credited");
        }

        public async Task<(bool Success, string Message)> RejectAsync(
            Guid requestId, string? reason, Guid? reviewerUserId)
        {
            var req = await _db.FeePaymentRequests.FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);
            if (req == null) return (false, "Request not found");
            if (req.Status != FeePaymentRequestStatus.Pending)
                return (false, $"Request is already {req.Status}");

            req.Status = FeePaymentRequestStatus.Rejected;
            req.RejectionReason = reason;
            req.ReviewedByUserId = reviewerUserId;
            req.ReviewedAt = DateTime.UtcNow;
            req.LastModified = DateTime.UtcNow;
            req.LastModifiedBy = reviewerUserId;
            await _uow.CompleteAsync();
            return (true, "Request rejected");
        }
    }
}
