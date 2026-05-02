using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IDepartmentService
    {
        Task<IList<Department>> GetAllAsync(EducationStream? stream = null);
        Task<Department?> GetByIdAsync(Guid id);

        Task<(bool Success, string Message, Department? Department)> CreateAsync(Department department, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Department department, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
    }
}
