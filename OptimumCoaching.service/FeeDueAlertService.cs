using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class FeeDueAlertService : IFeeDueAlertService
    {
        private const string TagFeeOverdue = "fee-overdue";

        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;
        private readonly INoticeSettingsService _noticeSettings;

        public FeeDueAlertService(
            ApplicationDbContext db, IUnitOfWork uow, INoticeSettingsService noticeSettings)
        {
            _db = db; _uow = uow; _noticeSettings = noticeSettings;
        }

        public async Task<IList<DueAlertRow>> GetForStudentAsync(Guid studentId)
        {
            var accounts = await _db.StudentFeeAccounts
                .Include(a => a.Student)
                .Include(a => a.Batch)
                .Where(a => !a.IsDeleted && a.StudentId == studentId)
                .Where(a => a.Status != FeeAccountStatus.PaidInFull && a.Status != FeeAccountStatus.Waived)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            return accounts
                .Select(a => BuildRow(a, today))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
        }

        public async Task<IList<DueAlertRow>> GetAllOverdueAsync()
        {
            var accounts = await _db.StudentFeeAccounts
                .Include(a => a.Student)
                .Include(a => a.Batch)
                .Where(a => !a.IsDeleted)
                .Where(a => a.Status != FeeAccountStatus.PaidInFull && a.Status != FeeAccountStatus.Waived)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            return accounts
                .Select(a => BuildRow(a, today))
                .Where(r => r != null)
                .Select(r => r!)
                .OrderByDescending(r => r.DaysOverdue)
                .ToList();
        }

        public async Task<int> EnsureAlertsForStudentAsync(Guid studentId, Guid? actorId)
        {
            var rows = await GetForStudentAsync(studentId);
            if (rows.Count == 0) return 0;

            var accounts = await _db.StudentFeeAccounts
                .Where(a => !a.IsDeleted && a.StudentId == studentId)
                .ToDictionaryAsync(a => a.Id);

            var settings = await _noticeSettings.GetAsync();
            var now = DateTime.UtcNow;
            var today = now.Date;
            int created = 0;

            foreach (var row in rows)
            {
                if (!accounts.TryGetValue(row.AccountId, out var account)) continue;
                // Throttle to one alert per day per account.
                if (account.LastDueAlertAt.HasValue && account.LastDueAlertAt.Value.Date >= today)
                    continue;

                var title = $"Fee overdue — {row.BatchName}";
                var body = $"Your course fee is {row.DaysOverdue} day(s) overdue. " +
                           $"Outstanding balance: {row.Balance:N2}" +
                           (row.LateFee > 0 ? $" + late fee {row.LateFee:N2} = {row.TotalOwed:N2}." : ".") +
                           " Please clear this dues as soon as possible.";

                _db.Notices.Add(new Notice
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Body = body,
                    Audience = NoticeAudience.Students,
                    StudentId = studentId,
                    IsPinned = settings.OverdueAlertPinned,
                    SystemTag = TagFeeOverdue,
                    ExpiresAt = today.AddDays(settings.OverdueAlertExpiryDays),
                    PostedAt = now,
                    PostedByUserId = actorId,
                    IsActive = true,
                    Created = now,
                    CreatedBy = actorId
                });

                account.LastDueAlertAt = now;
                created++;
            }

            if (created > 0) await _uow.CompleteAsync();
            return created;
        }

        private static DueAlertRow? BuildRow(StudentFeeAccount a, DateTime today)
        {
            if (a.Balance <= 0) return null;
            var dueDate = ResolveDueDate(a);
            if (!dueDate.HasValue) return null;          // no policy → not overdue by policy
            if (today <= dueDate.Value) return null;     // not yet overdue

            var daysOverdue = (today - dueDate.Value).Days;
            var lateFee = a.Batch.LateFeeFlat + (a.Batch.LateFeePerDay * daysOverdue);

            return new DueAlertRow
            {
                AccountId = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                StudentCode = a.Student.StudentCode,
                BatchId = a.BatchId,
                BatchName = a.Batch.Name,
                Balance = a.Balance,
                DueDate = dueDate.Value,
                DaysOverdue = daysOverdue,
                LateFee = lateFee
            };
        }

        // Absolute date wins; otherwise enrollment + days. Returns null when
        // no due policy is configured (the account is never "overdue").
        private static DateTime? ResolveDueDate(StudentFeeAccount a)
        {
            if (a.Batch.FeeDueDate.HasValue) return a.Batch.FeeDueDate.Value.Date;
            if (a.Batch.FeeDueDays.HasValue && a.Student.EnrollmentDate.HasValue)
                return a.Student.EnrollmentDate.Value.Date.AddDays(a.Batch.FeeDueDays.Value);
            return null;
        }
    }
}
