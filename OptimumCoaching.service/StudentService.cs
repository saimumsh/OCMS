using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class StudentService : IStudentService
    {
        private readonly IRepository<Student> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;
        private readonly IStudentCodeService _codeService;
        private readonly IFeeService _feeService;

        public StudentService(
            IRepository<Student> repo,
            IUnitOfWork uow,
            ApplicationDbContext db,
            IStudentCodeService codeService,
            IFeeService feeService)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
            _codeService = codeService;
            _feeService = feeService;
        }

        public async Task<IList<Student>> GetAllAsync(bool includeUser = false, StudentApprovalStatus? status = null)
        {
            IQueryable<Student> q = _db.Students.Where(s => !s.IsDeleted);
            if (status.HasValue) q = q.Where(s => s.ApprovalStatus == status.Value);
            if (includeUser) q = q.Include(s => s.User).Include(s => s.Guardian);
            return await q.OrderByDescending(s => s.Created).ToListAsync();
        }

        public Task<Student?> GetByIdAsync(Guid id) =>
            _db.Students.Include(s => s.Guardian).Include(s => s.User)
                .Include(s => s.AcademicRecords)
                .FirstOrDefaultAsync(s => s.Id == id);

        public Task<Student?> GetByUserIdAsync(Guid userId) =>
            _db.Students
                .Include(s => s.AcademicRecords)
                .Include(s => s.Department)
                .Include(s => s.Batch)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

        public async Task<(bool Success, string Message, Student? Student)> CreateAsync(Student student, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(student.FullName))
                return (false, "Full name is required", null);

            if (student.UserId.HasValue)
            {
                var taken = await _db.Students.AnyAsync(s =>
                    s.UserId == student.UserId && !s.IsDeleted);
                if (taken) return (false, "This user already has a student record", null);
            }

            student.Id = student.Id == Guid.Empty ? Guid.NewGuid() : student.Id;
            student.IsActive = true;
            student.IsDeleted = false;
            student.Created = DateTime.UtcNow;
            student.CreatedBy = createdBy;
            // self-registrations come in as Pending; admin-created come in as Approved.
            if (student.ApprovalStatus == StudentApprovalStatus.Approved && student.ApprovedAt == null)
            {
                student.ApprovedAt = DateTime.UtcNow;
                student.ApprovedBy = createdBy;
            }

            int order = 0;
            foreach (var r in student.AcademicRecords)
            {
                r.Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                r.SortOrder = order++;
                r.IsActive = true;
                r.IsDeleted = false;
                r.Created = DateTime.UtcNow;
                r.CreatedBy = createdBy;
            }

            await _repo.AddAsync(student);
            await _uow.CompleteAsync();
            return (true, student.ApprovalStatus == StudentApprovalStatus.Pending
                ? "Student registered — awaiting approval"
                : "Student created", student);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Student student, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(student.Id);
            if (existing == null || existing.IsDeleted) return (false, "Student not found");

            existing.FullName = student.FullName;
            existing.Email = student.Email;
            existing.PhoneNumber = student.PhoneNumber;
            existing.DateOfBirth = student.DateOfBirth;
            existing.Gender = student.Gender;
            existing.Address = student.Address;
            existing.GuardianId = student.GuardianId;
            existing.GuardianName = student.GuardianName;
            existing.GuardianPhone = student.GuardianPhone;
            existing.EnrollmentDate = student.EnrollmentDate;
            existing.DepartmentId = student.DepartmentId;
            existing.BatchId = student.BatchId;
            existing.Session = student.Session;
            existing.Notes = student.Notes;
            if (student.ImageUrl != null) existing.ImageUrl = string.IsNullOrWhiteSpace(student.ImageUrl) ? null : student.ImageUrl;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await ReplaceAcademicRecordsAsync(existing, student.AcademicRecords, lastModifiedBy);

            await _uow.CompleteAsync();

            // Best-effort: try to assign / refresh the auto code now that the
            // student's batch / session may have changed. Failures are silent —
            // the code can be assigned later when missing fields are filled in.
            await _codeService.AssignAsync(existing.Id);

            // Best-effort: ensure the per-batch fee account exists so the
            // Finance team can record payments immediately.
            if (existing.BatchId.HasValue)
                await _feeService.EnsureAccountAsync(existing.Id, existing.BatchId.Value, lastModifiedBy);

            return (true, "Student updated");
        }

        // Hard-replace strategy: drop all existing academic rows then insert the
        // submitted set. Simple and correct — the row count is small per student.
        private async Task ReplaceAcademicRecordsAsync(
            Student student, IEnumerable<StudentAcademicRecord> incoming, Guid? actor)
        {
            var existingRows = await _db.StudentAcademicRecords
                .Where(r => r.StudentId == student.Id).ToListAsync();
            _db.StudentAcademicRecords.RemoveRange(existingRows);

            int order = 0;
            foreach (var r in incoming)
            {
                r.Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                r.StudentId = student.Id;
                r.SortOrder = order++;
                r.IsActive = true;
                r.IsDeleted = false;
                r.Created = DateTime.UtcNow;
                r.CreatedBy = actor;
                _db.StudentAcademicRecords.Add(r);
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Student not found");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Student removed");
        }

        public async Task<(bool Success, string Message)> ApproveAsync(Guid id, Guid? approverId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Student not found");
            if (existing.ApprovalStatus == StudentApprovalStatus.Approved) return (false, "Already approved");

            existing.ApprovalStatus = StudentApprovalStatus.Approved;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.ApprovedBy = approverId;
            existing.RejectionReason = null;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = approverId;

            await _uow.CompleteAsync();

            // Try to assign the StudentCode now (only succeeds when the student
            // already has a Batch + Session — otherwise it's assigned later via
            // UpdateAsync once those fields are filled in).
            await _codeService.AssignAsync(existing.Id);

            // Create the fee account if the student already has a batch.
            if (existing.BatchId.HasValue)
                await _feeService.EnsureAccountAsync(existing.Id, existing.BatchId.Value, approverId);

            return (true, "Student approved");
        }

        public async Task<(bool Success, string Message)> RejectAsync(Guid id, string? reason, Guid? approverId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Student not found");

            existing.ApprovalStatus = StudentApprovalStatus.Rejected;
            existing.RejectionReason = reason;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.ApprovedBy = approverId;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = approverId;

            await _uow.CompleteAsync();
            return (true, "Student rejected");
        }
    }
}
