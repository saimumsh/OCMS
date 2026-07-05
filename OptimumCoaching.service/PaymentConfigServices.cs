using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class PaymentSettingsService : IPaymentSettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public PaymentSettingsService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public async Task<PaymentSettings> GetAsync()
        {
            var row = await _db.PaymentSettingsRows
                .OrderBy(s => s.Created)
                .FirstOrDefaultAsync(s => !s.IsDeleted);
            if (row != null) return row;

            row = new PaymentSettings
            {
                Id = Guid.NewGuid(),
                CurrencySymbol = "৳",
                ReceiptPrefix = "OCMS-",
                NextReceiptNumber = 1,
                EnabledMethodsCsv = null,
                IsActive = true,
                Created = DateTime.UtcNow
            };
            _db.PaymentSettingsRows.Add(row);
            await _uow.CompleteAsync();
            return row;
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            string currencySymbol, string receiptPrefix, int nextReceiptNumber,
            string? enabledMethodsCsv, Guid? actorId)
        {
            if (nextReceiptNumber < 1) return (false, "Next receipt number must be ≥ 1");

            var row = await GetAsync();
            row.CurrencySymbol = string.IsNullOrWhiteSpace(currencySymbol) ? "৳" : currencySymbol;
            row.ReceiptPrefix = receiptPrefix ?? string.Empty;
            row.NextReceiptNumber = nextReceiptNumber;
            row.EnabledMethodsCsv = string.IsNullOrWhiteSpace(enabledMethodsCsv) ? null : enabledMethodsCsv;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Payment settings saved");
        }

        public async Task<string> ReserveReceiptNumberAsync(Guid? actorId)
        {
            var row = await GetAsync();
            var num = row.NextReceiptNumber;
            row.NextReceiptNumber = num + 1;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return $"{row.ReceiptPrefix}{num:D6}";
        }

        public async Task<ISet<PaymentMethod>> GetEnabledMethodsAsync()
        {
            var row = await GetAsync();
            var all = Enum.GetValues<PaymentMethod>().ToHashSet();
            if (string.IsNullOrWhiteSpace(row.EnabledMethodsCsv)) return all;

            var enabled = row.EnabledMethodsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? (PaymentMethod?)((PaymentMethod)n) : null)
                .Where(m => m.HasValue && all.Contains(m.Value))
                .Select(m => m!.Value)
                .ToHashSet();

            // Defensive: empty parse → fall back to all enabled.
            return enabled.Count == 0 ? all : enabled;
        }
    }

    public class ResultDiscountTierService : IResultDiscountTierService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public ResultDiscountTierService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<ResultDiscountTier>> GetAllAsync() =>
            _db.ResultDiscountTiers
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.MinResultPercent)
                .ToListAsync()
                .ContinueWith(t => (IList<ResultDiscountTier>)t.Result);

        public async Task<(bool Success, string Message, ResultDiscountTier? Tier)> CreateAsync(
            string name, decimal minResultPercent, decimal discountPercent, Guid? actorId)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required", null);
            if (minResultPercent < 0 || minResultPercent > 100) return (false, "Result % must be between 0 and 100", null);
            if (discountPercent <= 0 || discountPercent > 100) return (false, "Discount % must be between 0 and 100", null);

            var row = new ResultDiscountTier
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                MinResultPercent = minResultPercent,
                DiscountPercent = discountPercent,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = actorId
            };
            _db.ResultDiscountTiers.Add(row);
            await _uow.CompleteAsync();
            return (true, "Tier added", row);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string name, decimal minResultPercent, decimal discountPercent, Guid? actorId)
        {
            var row = await _db.ResultDiscountTiers.FirstOrDefaultAsync(t => t.Id == id);
            if (row == null || row.IsDeleted) return (false, "Tier not found");
            if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required");
            if (minResultPercent < 0 || minResultPercent > 100) return (false, "Result % must be between 0 and 100");
            if (discountPercent <= 0 || discountPercent > 100) return (false, "Discount % must be between 0 and 100");

            row.Name = name.Trim();
            row.MinResultPercent = minResultPercent;
            row.DiscountPercent = discountPercent;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Tier updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var row = await _db.ResultDiscountTiers.FirstOrDefaultAsync(t => t.Id == id);
            if (row == null || row.IsDeleted) return (false, "Tier not found");
            row.IsDeleted = true;
            row.IsActive = false;
            row.LastModified = DateTime.UtcNow;
            row.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Tier removed");
        }

        public Task<ResultDiscountTier?> FindForResultAsync(decimal resultPercent) =>
            _db.ResultDiscountTiers
                .Where(t => !t.IsDeleted && t.MinResultPercent <= resultPercent)
                .OrderByDescending(t => t.MinResultPercent)
                .FirstOrDefaultAsync();
    }
}
