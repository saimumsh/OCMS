using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface ISubjectService
    {
        Task<IList<Subject>> GetAllAsync(Guid? departmentId = null);
        Task<Subject?> GetByIdAsync(Guid id);

        Task<(bool Success, string Message, Subject? Subject)> CreateAsync(Subject subject, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Subject subject, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
    }
}
