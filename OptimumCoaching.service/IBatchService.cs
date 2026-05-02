using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IBatchService
    {
        Task<IList<Batch>> GetAllAsync(Guid? departmentId = null);
        Task<Batch?> GetByIdAsync(Guid id);
        Task<IList<Batch>> GetForTeacherAsync(Guid teacherId);

        Task<(bool Success, string Message, Batch? Batch)> CreateAsync(Batch batch, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Batch batch, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
    }
}
