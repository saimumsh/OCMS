using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class TeacherService : ITeacherService
    {
        private readonly IRepository<Teacher> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public TeacherService(IRepository<Teacher> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Teacher>> GetAllAsync(bool includeUser = false)
        {
            IQueryable<Teacher> q = _db.Teachers.Where(t => !t.IsDeleted);
            if (includeUser) q = q.Include(t => t.User);
            return await q.OrderBy(t => t.FullName).ToListAsync();
        }

        public Task<Teacher?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public Task<Teacher?> GetByUserIdAsync(Guid userId) =>
            _db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);

        public async Task<(bool Success, string Message, Teacher? Teacher)> CreateAsync(Teacher teacher, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(teacher.FullName))
                return (false, "Full name is required", null);

            if (teacher.UserId.HasValue)
            {
                var taken = await _db.Teachers.AnyAsync(t =>
                    t.UserId == teacher.UserId && !t.IsDeleted);
                if (taken) return (false, "This user is already linked to a teacher record", null);
            }

            teacher.Id = teacher.Id == Guid.Empty ? Guid.NewGuid() : teacher.Id;
            teacher.IsActive = true;
            teacher.IsDeleted = false;
            teacher.Created = DateTime.UtcNow;
            teacher.CreatedBy = createdBy;

            await _repo.AddAsync(teacher);
            await _uow.CompleteAsync();
            return (true, "Teacher created", teacher);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Teacher teacher, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(teacher.Id);
            if (existing == null || existing.IsDeleted) return (false, "Teacher not found");

            existing.FullName = teacher.FullName;
            existing.Email = teacher.Email;
            existing.PhoneNumber = teacher.PhoneNumber;
            existing.DateOfBirth = teacher.DateOfBirth;
            existing.Gender = teacher.Gender;
            existing.Address = teacher.Address;
            existing.Specialization = teacher.Specialization;
            existing.Qualification = teacher.Qualification;
            existing.ExperienceYears = teacher.ExperienceYears;
            existing.HireDate = teacher.HireDate;
            existing.Bio = teacher.Bio;
            if (teacher.ImageUrl != null) existing.ImageUrl = string.IsNullOrWhiteSpace(teacher.ImageUrl) ? null : teacher.ImageUrl;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Teacher updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Teacher not found");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Teacher removed");
        }

        public async Task<(bool Success, string Message)> SetActiveAsync(Guid id, bool isActive, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Teacher not found");

            existing.IsActive = isActive;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, isActive ? "Teacher activated" : "Teacher deactivated");
        }
    }
}
