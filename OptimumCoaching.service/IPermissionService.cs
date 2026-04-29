using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IPermissionService
    {
        Task<IList<Permission>> GetAllAsync();
        Task<Permission?> FindByIdAsync(Guid id);
        Task<Permission?> FindByNameAsync(string name);
        Task<(bool Success, string Message, Permission? Permission)> CreateAsync(
            string name, string displayName, string? description, string? category);
        Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string displayName, string? description, string? category, bool isActive);
        Task<(bool Success, string Message)> DeleteAsync(Guid id);

        Task<IList<Permission>> GetByRoleAsync(Guid roleId);
        Task<IList<string>> GetPermissionNamesByRoleAsync(Guid roleId);
        Task<(bool Success, string Message)> AssignToRoleAsync(Guid roleId, Guid permissionId);
        Task<(bool Success, string Message)> RemoveFromRoleAsync(Guid roleId, Guid permissionId);
        Task<(bool Success, string Message)> SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);

        Task<IList<string>> GetPermissionNamesForUserAsync(Guid userId);
    }
}
