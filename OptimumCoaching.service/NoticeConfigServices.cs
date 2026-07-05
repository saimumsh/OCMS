using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class NoticeSettingsService : INoticeSettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public NoticeSettingsService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public async Task<NoticeSettings> GetAsync()
        {
            var row = await _db.NoticeSettingsRows
                .OrderBy(s => s.Created)
                .FirstOrDefaultAsync(s => !s.IsDeleted);
            if (row != null) return row;

            row = new NoticeSettings
            {
                Id = Guid.NewGuid(),
                DefaultAudience = NoticeAudience.Both,
                DefaultExpiryDays = 30,
                DefaultPinned = false,
                OverdueAlertPinned = true,
                OverdueAlertExpiryDays = 14,
                IsActive = true,
                Created = DateTime.UtcNow
            };
            _db.NoticeSettingsRows.Add(row);
            await _uow.CompleteAsync();
            return row;
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            NoticeAudience defaultAudience, int defaultExpiryDays, bool defaultPinned,
            bool overdueAlertPinned, int overdueAlertExpiryDays, Guid? actorId)
        {
            if (defaultExpiryDays < 0 || defaultExpiryDays > 365)
                return (false, "Default expiry must be between 0 and 365 days");
            if (overdueAlertExpiryDays < 1 || overdueAlertExpiryDays > 365)
                return (false, "Overdue alert expiry must be between 1 and 365 days");

            var row = await GetAsync();
            row.DefaultAudience = defaultAudience;
            row.DefaultExpiryDays = defaultExpiryDays;
            row.DefaultPinned = defaultPinned;
            row.OverdueAlertPinned = overdueAlertPinned;
            row.OverdueAlertExpiryDays = overdueAlertExpiryDays;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Notice settings saved");
        }
    }

    public class NoticeTemplateService : INoticeTemplateService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public NoticeTemplateService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<NoticeTemplate>> GetAllAsync() =>
            _db.NoticeTemplates
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync()
                .ContinueWith(t => (IList<NoticeTemplate>)t.Result);

        public Task<NoticeTemplate?> GetByIdAsync(Guid id) =>
            _db.NoticeTemplates.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        public async Task<(bool Success, string Message, NoticeTemplate? Template)> CreateAsync(
            string name, string title, string body, NoticeAudience audience, Guid? actorId)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required", null);
            if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required", null);
            if (string.IsNullOrWhiteSpace(body)) return (false, "Body is required", null);

            var row = new NoticeTemplate
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Title = title.Trim(),
                Body = body,
                DefaultAudience = audience,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = actorId
            };
            _db.NoticeTemplates.Add(row);
            await _uow.CompleteAsync();
            return (true, "Template added", row);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string name, string title, string body, NoticeAudience audience, Guid? actorId)
        {
            var row = await _db.NoticeTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (row == null || row.IsDeleted) return (false, "Template not found");
            if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required");
            if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required");
            if (string.IsNullOrWhiteSpace(body)) return (false, "Body is required");

            row.Name = name.Trim();
            row.Title = title.Trim();
            row.Body = body;
            row.DefaultAudience = audience;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Template updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var row = await _db.NoticeTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (row == null || row.IsDeleted) return (false, "Template not found");
            row.IsDeleted = true;
            row.IsActive = false;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Template removed");
        }
    }
}
