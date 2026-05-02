using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class BatchService : IBatchService
    {
        private readonly IRepository<Batch> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public BatchService(IRepository<Batch> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Batch>> GetAllAsync(Guid? departmentId = null)
        {
            IQueryable<Batch> q = _db.Batches.Where(b => !b.IsDeleted);
            if (departmentId.HasValue) q = q.Where(b => b.DepartmentId == departmentId.Value);
            return await q
                .Include(b => b.Department)
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.Teacher)
                .OrderBy(b => b.Department!.Name).ThenBy(b => b.Name)
                .ToListAsync();
        }

        public Task<Batch?> GetByIdAsync(Guid id) =>
            _db.Batches
                .Include(b => b.Department)
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.Teacher)
                .FirstOrDefaultAsync(b => b.Id == id);

        public Task<IList<Batch>> GetForTeacherAsync(Guid teacherId) =>
            _db.Batches
                .Where(b => !b.IsDeleted && b.TeacherId == teacherId)
                .Include(b => b.Department)
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .OrderBy(b => b.Name)
                .ToListAsync()
                .ContinueWith(t => (IList<Batch>)t.Result);

        public async Task<(bool Success, string Message, Batch? Batch)> CreateAsync(Batch batch, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(batch.Name))
                return (false, "Name is required", null);

            if (!string.IsNullOrWhiteSpace(batch.Code))
            {
                var dup = await _db.Batches.AnyAsync(b => !b.IsDeleted && b.Code == batch.Code);
                if (dup) return (false, $"Code '{batch.Code}' is already in use", null);
            }

            batch.Id = batch.Id == Guid.Empty ? Guid.NewGuid() : batch.Id;
            batch.IsActive = true;
            batch.IsDeleted = false;
            batch.Created = DateTime.UtcNow;
            batch.CreatedBy = createdBy;

            await _repo.AddAsync(batch);
            await _uow.CompleteAsync();
            return (true, "Batch created", batch);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Batch batch, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(batch.Id);
            if (existing == null || existing.IsDeleted) return (false, "Batch not found");

            if (!string.IsNullOrWhiteSpace(batch.Code))
            {
                var dup = await _db.Batches.AnyAsync(b =>
                    b.Id != batch.Id && !b.IsDeleted && b.Code == batch.Code);
                if (dup) return (false, $"Code '{batch.Code}' is already in use");
            }

            existing.Name = batch.Name;
            existing.Code = batch.Code;
            existing.Description = batch.Description;
            existing.DepartmentId = batch.DepartmentId;
            existing.ClassId = batch.ClassId;
            existing.SubjectId = batch.SubjectId;
            existing.TeacherId = batch.TeacherId;
            existing.StartDate = batch.StartDate;
            existing.EndDate = batch.EndDate;
            existing.Capacity = batch.Capacity;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Batch updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Batch not found");

            var hasStudents = await _db.Students.AnyAsync(s => s.BatchId == id && !s.IsDeleted);
            if (hasStudents) return (false, "Cannot delete — students are assigned to this batch");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Batch removed");
        }
    }
}
