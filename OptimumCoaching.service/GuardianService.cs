using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class GuardianService : IGuardianService
    {
        private readonly IRepository<Guardian> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public GuardianService(IRepository<Guardian> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo;
            _uow = uow;
            _db = db;
        }

        public async Task<IList<Guardian>> GetAllAsync(bool includeUser = false)
        {
            IQueryable<Guardian> q = _db.Guardians.Where(g => !g.IsDeleted);
            if (includeUser) q = q.Include(g => g.User);
            return await q.OrderBy(g => g.FullName).ToListAsync();
        }

        public Task<Guardian?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public Task<Guardian?> GetByUserIdAsync(Guid userId) =>
            _db.Guardians.FirstOrDefaultAsync(g => g.UserId == userId && !g.IsDeleted);

        public async Task<(bool Success, string Message, Guardian? Guardian)> CreateAsync(Guardian guardian, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(guardian.FullName))
                return (false, "Full name is required", null);
            if (string.IsNullOrWhiteSpace(guardian.PhoneNumber))
                return (false, "Phone number is required", null);

            if (guardian.UserId.HasValue)
            {
                var taken = await _db.Guardians.AnyAsync(g =>
                    g.UserId == guardian.UserId && !g.IsDeleted);
                if (taken) return (false, "This user already has a guardian record", null);
            }

            guardian.Id = guardian.Id == Guid.Empty ? Guid.NewGuid() : guardian.Id;
            guardian.IsActive = true;
            guardian.IsDeleted = false;
            guardian.Created = DateTime.UtcNow;
            guardian.CreatedBy = createdBy;

            await _repo.AddAsync(guardian);
            await _uow.CompleteAsync();
            return (true, "Guardian created", guardian);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Guardian guardian, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(guardian.Id);
            if (existing == null || existing.IsDeleted) return (false, "Guardian not found");

            existing.FullName = guardian.FullName;
            existing.Email = guardian.Email;
            existing.PhoneNumber = guardian.PhoneNumber;
            existing.Relationship = guardian.Relationship;
            existing.Occupation = guardian.Occupation;
            existing.Address = guardian.Address;
            existing.Notes = guardian.Notes;
            if (guardian.ImageUrl != null) existing.ImageUrl = string.IsNullOrWhiteSpace(guardian.ImageUrl) ? null : guardian.ImageUrl;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Guardian updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Guardian not found");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Guardian removed");
        }
    }
}
