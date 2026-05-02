using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface ITeacherService
    {
        Task<IList<Teacher>> GetAllAsync(bool includeUser = false);
        Task<Teacher?> GetByIdAsync(Guid id);
        Task<Teacher?> GetByUserIdAsync(Guid userId);

        Task<(bool Success, string Message, Teacher? Teacher)> CreateAsync(Teacher teacher, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Teacher teacher, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> SetActiveAsync(Guid id, bool isActive, Guid? lastModifiedBy);
    }
}
