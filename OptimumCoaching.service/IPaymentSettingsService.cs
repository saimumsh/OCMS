using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IPaymentSettingsService
    {
        // Returns the singleton row; creates it on first access.
        Task<PaymentSettings> GetAsync();

        Task<(bool Success, string Message)> UpdateAsync(
            string currencySymbol, string receiptPrefix, int nextReceiptNumber,
            string? enabledMethodsCsv, Guid? actorId);

        // Consumes the next sequence value and returns the formatted
        // receipt number (e.g. "OCMS-2026-000123"). Persists the bump.
        Task<string> ReserveReceiptNumberAsync(Guid? actorId);

        // Convenience: returns the set of methods currently enabled. Empty
        // CSV / null means all methods are enabled.
        Task<ISet<PaymentMethod>> GetEnabledMethodsAsync();
    }

    public interface IResultDiscountTierService
    {
        Task<IList<ResultDiscountTier>> GetAllAsync();

        Task<(bool Success, string Message, ResultDiscountTier? Tier)> CreateAsync(
            string name, decimal minResultPercent, decimal discountPercent, Guid? actorId);

        Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string name, decimal minResultPercent, decimal discountPercent, Guid? actorId);

        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);

        // Finds the best-fit tier for the given percent (largest min ≤ percent).
        Task<ResultDiscountTier?> FindForResultAsync(decimal resultPercent);
    }
}
