using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IApplicationRoleService
    {
        Task<IList<ApplicationRole>> GetAllAsync();
        Task<ApplicationRole?> FindByIdAsync(Guid id);
        Task<ApplicationRole?> FindByNameAsync(string roleName);
        Task<(bool Success, string Message, ApplicationRole? Role)> CreateRoleAsync(string roleName);
        Task<(bool Success, string Message)> RenameAsync(Guid id, string newName);
        Task<(bool Success, string Message)> DeleteAsync(Guid id);
    }
}
