using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class NoticeService : INoticeService
    {
        private readonly IRepository<Notice> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public NoticeService(IRepository<Notice> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Notice>> GetAllAsync(NoticeAudience? audience = null)
        {
            IQueryable<Notice> q = _db.Notices
                .Include(n => n.Department)
                .Include(n => n.PostedByUser)
                .Where(n => !n.IsDeleted);

            if (audience.HasValue)
            {
                // Audience.Both is visible to everyone, so show it for any filter.
                q = q.Where(n => n.Audience == audience.Value || n.Audience == NoticeAudience.Both);
            }

            return await q
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PostedAt)
                .ToListAsync();
        }

        public Task<IList<Notice>> GetForReceiverAsync(
            NoticeAudience audience, Guid? departmentId, int take = 20)
            => GetForReceiverAsync(audience, departmentId, studentId: null, take);

        // Overload that filters out per-student targeted notices to only the
        // current viewer's StudentId — passes null when not a Student.
        public async Task<IList<Notice>> GetForReceiverAsync(
            NoticeAudience audience, Guid? departmentId, Guid? studentId, int take = 20)
        {
            var now = DateTime.UtcNow;

            var q = _db.Notices
                .Include(n => n.Department)
                .Include(n => n.PostedByUser)
                .Where(n => !n.IsDeleted && n.IsActive)
                .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt > now)
                // The receiver only sees notices targeted at them or at "Both".
                .Where(n => n.Audience == audience || n.Audience == NoticeAudience.Both)
                // Department-scoped notices are only shown to the same department;
                // notices with no department apply to everyone.
                .Where(n => n.DepartmentId == null || n.DepartmentId == departmentId)
                // Per-student notices are only shown to the matching student.
                .Where(n => n.StudentId == null || n.StudentId == studentId);

            return await q
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PostedAt)
                .Take(take)
                .ToListAsync();
        }

        public Task<Notice?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, Notice? Notice)> CreateAsync(
            Notice notice, Guid? postedByUserId)
        {
            if (string.IsNullOrWhiteSpace(notice.Title))
                return (false, "Title is required", null);
            if (string.IsNullOrWhiteSpace(notice.Body))
                return (false, "Message is required", null);
            if (notice.ExpiresAt.HasValue && notice.ExpiresAt <= DateTime.UtcNow)
                return (false, "Expiry date must be in the future", null);

            notice.Id = notice.Id == Guid.Empty ? Guid.NewGuid() : notice.Id;
            notice.PostedAt = DateTime.UtcNow;
            notice.PostedByUserId = postedByUserId;
            notice.IsActive = true;
            notice.IsDeleted = false;
            notice.Created = DateTime.UtcNow;
            notice.CreatedBy = postedByUserId;

            await _repo.AddAsync(notice);
            await _uow.CompleteAsync();
            return (true, "Notice posted", notice);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Notice notice, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(notice.Id);
            if (existing == null || existing.IsDeleted) return (false, "Notice not found");

            if (string.IsNullOrWhiteSpace(notice.Title)) return (false, "Title is required");
            if (string.IsNullOrWhiteSpace(notice.Body)) return (false, "Message is required");
            if (notice.ExpiresAt.HasValue && notice.ExpiresAt < DateTime.UtcNow.AddMinutes(-1))
                return (false, "Expiry date cannot be in the past");

            existing.Title = notice.Title;
            existing.Body = notice.Body;
            existing.Audience = notice.Audience;
            existing.DepartmentId = notice.DepartmentId;
            existing.IsPinned = notice.IsPinned;
            existing.ExpiresAt = notice.ExpiresAt;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Notice updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Notice not found");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Notice removed");
        }
    }
}
