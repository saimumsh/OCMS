using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface INoticeSettingsService
    {
        Task<NoticeSettings> GetAsync();
        Task<(bool Success, string Message)> UpdateAsync(
            NoticeAudience defaultAudience, int defaultExpiryDays, bool defaultPinned,
            bool overdueAlertPinned, int overdueAlertExpiryDays, Guid? actorId);
    }

    public interface INoticeTemplateService
    {
        Task<IList<NoticeTemplate>> GetAllAsync();
        Task<NoticeTemplate?> GetByIdAsync(Guid id);
        Task<(bool Success, string Message, NoticeTemplate? Template)> CreateAsync(
            string name, string title, string body, NoticeAudience audience, Guid? actorId);
        Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string name, string title, string body, NoticeAudience audience, Guid? actorId);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);
    }
}
