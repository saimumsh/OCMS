using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;

namespace OptimumCoaching.service
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _db;

        public PermissionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<IList<Permission>> GetAllAsync() =>
            _db.Permissions.OrderBy(p => p.Category).ThenBy(p => p.Name)
                .ToListAsync().ContinueWith(t => (IList<Permission>)t.Result);

        public Task<Permission?> FindByIdAsync(Guid id) =>
            _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);

        public Task<Permission?> FindByNameAsync(string name) =>
            _db.Permissions.FirstOrDefaultAsync(p => p.Name == name);

        public async Task<(bool Success, string Message, Permission? Permission)> CreateAsync(
            string name, string displayName, string? description, string? category)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Name is required", null);

            if (await _db.Permissions.AnyAsync(p => p.Name == name))
                return (false, "Permission with that name already exists", null);

            var perm = new Permission
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? name.Trim() : displayName.Trim(),
                Description = description,
                Category = category,
                IsActive = true,
                Created = DateTime.UtcNow
            };
            _db.Permissions.Add(perm);
            await _db.SaveChangesAsync();
            return (true, "Permission created", perm);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Guid id, string displayName, string? description, string? category, bool isActive)
        {
            var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
            if (perm == null) return (false, "Permission not found");

            perm.DisplayName = displayName;
            perm.Description = description;
            perm.Category = category;
            perm.IsActive = isActive;
            await _db.SaveChangesAsync();
            return (true, "Permission updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
        {
            var perm = await _db.Permissions.Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (perm == null) return (false, "Permission not found");

            _db.RolePermissions.RemoveRange(perm.RolePermissions);
            _db.Permissions.Remove(perm);
            await _db.SaveChangesAsync();
            return (true, "Permission deleted");
        }

        public async Task<IList<Permission>> GetByRoleAsync(Guid roleId)
        {
            return await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .OrderBy(p => p.Category).ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IList<string>> GetPermissionNamesByRoleAsync(Guid roleId)
        {
            return await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> AssignToRoleAsync(Guid roleId, Guid permissionId)
        {
            var exists = await _db.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (exists) return (false, "Already assigned");

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                Created = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return (true, "Permission assigned");
        }

        public async Task<(bool Success, string Message)> RemoveFromRoleAsync(Guid roleId, Guid permissionId)
        {
            var rp = await _db.RolePermissions
                .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
            if (rp == null) return (false, "Not assigned");

            _db.RolePermissions.Remove(rp);
            await _db.SaveChangesAsync();
            return (true, "Permission removed");
        }

        public async Task<(bool Success, string Message)> SetRolePermissionsAsync(
            Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var ids = permissionIds.Distinct().ToHashSet();
            var current = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();

            var toRemove = current.Where(rp => !ids.Contains(rp.PermissionId)).ToList();
            _db.RolePermissions.RemoveRange(toRemove);

            var existingIds = current.Select(rp => rp.PermissionId).ToHashSet();
            foreach (var pid in ids.Where(id => !existingIds.Contains(id)))
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pid,
                    Created = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
            return (true, "Role permissions updated");
        }

        public async Task<IList<string>> GetPermissionNamesForUserAsync(Guid userId)
        {
            return await (from ur in _db.Set<ApplicationUserRole>()
                          join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                          where ur.UserId == userId && rp.Permission.IsActive
                          select rp.Permission.Name)
                          .Distinct()
                          .ToListAsync();
        }
    }
}
