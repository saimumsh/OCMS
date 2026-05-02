using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IGuardianService
    {
        Task<IList<Guardian>> GetAllAsync(bool includeUser = false);
        Task<Guardian?> GetByIdAsync(Guid id);
        Task<Guardian?> GetByUserIdAsync(Guid userId);

        Task<(bool Success, string Message, Guardian? Guardian)> CreateAsync(Guardian guardian, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Guardian guardian, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
    }
}
