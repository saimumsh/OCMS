using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public class ApplicationRoleService : IApplicationRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationRoleService(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public Task<IList<ApplicationRole>> GetAllAsync() =>
            _roleManager.Roles.OrderBy(r => r.Name).ToListAsync()
                .ContinueWith(t => (IList<ApplicationRole>)t.Result);

        public Task<ApplicationRole?> FindByIdAsync(Guid id) =>
            _roleManager.FindByIdAsync(id.ToString());

        public Task<ApplicationRole?> FindByNameAsync(string roleName) =>
            _roleManager.FindByNameAsync(roleName);

        public async Task<(bool Success, string Message, ApplicationRole? Role)> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return (false, "Role name is required", null);

            if (await _roleManager.RoleExistsAsync(roleName))
                return (false, "Role already exists", null);

            var role = new ApplicationRole(roleName);
            var result = await _roleManager.CreateAsync(role);
            return result.Succeeded
                ? (true, "Role created", role)
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        public async Task<(bool Success, string Message)> RenameAsync(Guid id, string newName)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return (false, "Role not found");

            if (Roles.All.Contains(role.Name))
                return (false, "System roles cannot be renamed");

            role.Name = newName;
            var result = await _roleManager.UpdateAsync(role);
            return result.Succeeded
                ? (true, "Role renamed")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return (false, "Role not found");

            if (Roles.All.Contains(role.Name))
                return (false, "System roles cannot be deleted");

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded
                ? (true, "Role deleted")
                : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
