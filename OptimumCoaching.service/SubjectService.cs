using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class SubjectService : ISubjectService
    {
        private readonly IRepository<Subject> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public SubjectService(IRepository<Subject> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Subject>> GetAllAsync(Guid? departmentId = null)
        {
            IQueryable<Subject> q = _db.Subjects
                .Include(s => s.Department)
                .Include(s => s.Class)
                .Where(s => !s.IsDeleted);

            if (departmentId.HasValue)
                q = q.Where(s => s.DepartmentId == departmentId.Value);

            return await q
                .OrderBy(s => s.Department!.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        public Task<Subject?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, Subject? Subject)> CreateAsync(
            Subject subject, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(subject.Name))
                return (false, "Name is required", null);

            // Unique per department + name (case-insensitive via SQL collation default).
            var dupName = await _db.Subjects.AnyAsync(s =>
                !s.IsDeleted &&
                s.DepartmentId == subject.DepartmentId &&
                s.Name == subject.Name);
            if (dupName)
                return (false, "A subject with this name already exists in the selected department", null);

            if (!string.IsNullOrWhiteSpace(subject.Code))
            {
                var dupCode = await _db.Subjects.AnyAsync(s =>
                    !s.IsDeleted && s.Code == subject.Code);
                if (dupCode) return (false, $"Code '{subject.Code}' is already in use", null);
            }

            subject.Id = subject.Id == Guid.Empty ? Guid.NewGuid() : subject.Id;
            subject.IsActive = true;
            subject.IsDeleted = false;
            subject.Created = DateTime.UtcNow;
            subject.CreatedBy = createdBy;

            await _repo.AddAsync(subject);
            await _uow.CompleteAsync();
            return (true, "Subject created", subject);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Subject subject, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(subject.Id);
            if (existing == null || existing.IsDeleted) return (false, "Subject not found");

            var dupName = await _db.Subjects.AnyAsync(s =>
                s.Id != subject.Id && !s.IsDeleted &&
                s.DepartmentId == subject.DepartmentId &&
                s.Name == subject.Name);
            if (dupName)
                return (false, "A subject with this name already exists in the selected department");

            if (!string.IsNullOrWhiteSpace(subject.Code))
            {
                var dupCode = await _db.Subjects.AnyAsync(s =>
                    s.Id != subject.Id && !s.IsDeleted && s.Code == subject.Code);
                if (dupCode) return (false, $"Code '{subject.Code}' is already in use");
            }

            existing.Name = subject.Name;
            existing.Code = subject.Code;
            existing.Description = subject.Description;
            existing.DepartmentId = subject.DepartmentId;
            existing.ClassId = subject.ClassId;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Subject updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Subject not found");

            var inUse = await _db.Batches.AnyAsync(b => b.SubjectId == id && !b.IsDeleted);
            if (inUse) return (false, "Cannot delete — batches are linked to this subject");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Subject removed");
        }
    }
}
