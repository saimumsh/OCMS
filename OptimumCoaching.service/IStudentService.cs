using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IStudentService
    {
        Task<IList<Student>> GetAllAsync(bool includeUser = false, StudentApprovalStatus? status = null);
        Task<Student?> GetByIdAsync(Guid id);
        Task<Student?> GetByUserIdAsync(Guid userId);

        Task<(bool Success, string Message, Student? Student)> CreateAsync(Student student, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Student student, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);

        Task<(bool Success, string Message)> ApproveAsync(Guid id, Guid? approverId);
        Task<(bool Success, string Message)> RejectAsync(Guid id, string? reason, Guid? approverId);
    }
}
