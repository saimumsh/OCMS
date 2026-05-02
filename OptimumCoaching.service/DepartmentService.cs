using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public DepartmentService(IRepository<Department> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Department>> GetAllAsync(EducationStream? stream = null)
        {
            IQueryable<Department> q = _db.Departments.Where(d => !d.IsDeleted);
            if (stream.HasValue) q = q.Where(d => d.Stream == stream.Value);
            return await q.OrderBy(d => d.Stream).ThenBy(d => d.Name).ToListAsync();
        }

        public Task<Department?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<(bool Success, string Message, Department? Department)> CreateAsync(Department department, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(department.Name))
                return (false, "Name is required", null);

            var dupName = await _db.Departments.AnyAsync(d =>
                !d.IsDeleted && d.Stream == department.Stream && d.Name == department.Name);
            if (dupName) return (false, $"A {department.Stream} department named '{department.Name}' already exists", null);

            if (!string.IsNullOrWhiteSpace(department.Code))
            {
                var dupCode = await _db.Departments.AnyAsync(d =>
                    !d.IsDeleted && d.Code == department.Code);
                if (dupCode) return (false, $"Code '{department.Code}' is already in use", null);
            }

            department.Id = department.Id == Guid.Empty ? Guid.NewGuid() : department.Id;
            department.IsActive = true;
            department.IsDeleted = false;
            department.Created = DateTime.UtcNow;
            department.CreatedBy = createdBy;

            await _repo.AddAsync(department);
            await _uow.CompleteAsync();
            return (true, "Department created", department);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Department department, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(department.Id);
            if (existing == null || existing.IsDeleted) return (false, "Department not found");

            var dupName = await _db.Departments.AnyAsync(d =>
                d.Id != department.Id && !d.IsDeleted &&
                d.Stream == department.Stream && d.Name == department.Name);
            if (dupName) return (false, $"A {department.Stream} department named '{department.Name}' already exists");

            if (!string.IsNullOrWhiteSpace(department.Code))
            {
                var dupCode = await _db.Departments.AnyAsync(d =>
                    d.Id != department.Id && !d.IsDeleted && d.Code == department.Code);
                if (dupCode) return (false, $"Code '{department.Code}' is already in use");
            }

            existing.Name = department.Name;
            existing.Code = department.Code;
            existing.Description = department.Description;
            existing.Stream = department.Stream;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Department updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Department not found");

            var inUse = await _db.Classes.AnyAsync(c => c.DepartmentId == id && !c.IsDeleted);
            if (inUse) return (false, "Cannot delete — classes are linked to this department");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Department removed");
        }
    }
}
