using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface INoticeService
    {
        // Admin/teacher management view — all non-deleted notices, optionally
        // filtered by audience.
        Task<IList<Notice>> GetAllAsync(NoticeAudience? audience = null);

        // Receiver view — notices the given audience should currently see
        // (not expired, not deleted, optional department match).
        Task<IList<Notice>> GetForReceiverAsync(NoticeAudience audience, Guid? departmentId, int take = 20);

        Task<Notice?> GetByIdAsync(Guid id);

        Task<(bool Success, string Message, Notice? Notice)> CreateAsync(Notice notice, Guid? postedByUserId);
        Task<(bool Success, string Message)> UpdateAsync(Notice notice, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
    }
}
