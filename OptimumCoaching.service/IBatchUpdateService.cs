using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IBatchUpdateService
    {
        Task<IList<BatchUpdate>> GetForBatchAsync(Guid batchId, int take = 50);
        Task<BatchUpdate?> GetByIdAsync(Guid id);

        Task<(bool Success, string Message, BatchUpdate? Update)> PostAsync(
            Guid batchId, string title, string body, Guid? postedByUserId);

        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actor);
    }
}
