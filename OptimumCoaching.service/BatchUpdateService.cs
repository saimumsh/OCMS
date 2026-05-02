using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class BatchUpdateService : IBatchUpdateService
    {
        private readonly IRepository<BatchUpdate> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public BatchUpdateService(IRepository<BatchUpdate> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public Task<IList<BatchUpdate>> GetForBatchAsync(Guid batchId, int take = 50) =>
            _db.BatchUpdates
                .Where(u => u.BatchId == batchId && !u.IsDeleted)
                .Include(u => u.PostedByUser)
                .OrderByDescending(u => u.PostedAt)
                .Take(take)
                .ToListAsync()
                .ContinueWith(t => (IList<BatchUpdate>)t.Result);

        public Task<BatchUpdate?> GetByIdAsync(Guid id) =>
            _db.BatchUpdates.Include(u => u.PostedByUser).FirstOrDefaultAsync(u => u.Id == id);

        public async Task<(bool Success, string Message, BatchUpdate? Update)> PostAsync(
            Guid batchId, string title, string body, Guid? postedByUserId)
        {
            if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required", null);
            if (string.IsNullOrWhiteSpace(body)) return (false, "Body is required", null);

            var batchExists = await _db.Batches.AnyAsync(b => b.Id == batchId && !b.IsDeleted);
            if (!batchExists) return (false, "Batch not found", null);

            var update = new BatchUpdate
            {
                Id = Guid.NewGuid(),
                BatchId = batchId,
                Title = title.Trim(),
                Body = body.Trim(),
                PostedAt = DateTime.UtcNow,
                PostedByUserId = postedByUserId,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = postedByUserId
            };

            await _repo.AddAsync(update);
            await _uow.CompleteAsync();
            return (true, "Update posted", update);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actor)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Update not found");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actor;

            await _uow.CompleteAsync();
            return (true, "Update removed");
        }
    }
}
