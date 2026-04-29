using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OptimumCoaching.web.Core
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var config = sp.GetRequiredService<IConfiguration>();

            // 1) Roles
            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new ApplicationRole(role));
            }

            // 2) Permission catalog → DB rows
            await SyncPermissionCatalogAsync(db);

            // 3) Default SuperAdmin user
            var superEmail = config["SeedUsers:SuperAdminEmail"] ?? "superadmin@optimumcoaching.local";
            var superPassword = config["SeedUsers:SuperAdminPassword"] ?? "SuperAdmin@123";
            await EnsureUserAsync(userManager, superEmail, "Super Admin", superPassword, Roles.SuperAdmin);

            // 4) Default Dev user
            var devEmail = config["SeedUsers:DevEmail"] ?? "dev@optimumcoaching.local";
            var devPassword = config["SeedUsers:DevPassword"] ?? "Dev@12345";
            await EnsureUserAsync(userManager, devEmail, "Developer", devPassword, Roles.Dev);
        }

        // Walks the static Permissions catalog via reflection. Each nested class
        // becomes a Category; each `public const string` field becomes a Permission row.
        private static async Task SyncPermissionCatalogAsync(ApplicationDbContext db)
        {
            var existing = await db.Permissions.ToListAsync();
            var existingByName = existing.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, displayName, category) in EnumerateCatalog())
            {
                seen.Add(name);

                if (existingByName.TryGetValue(name, out var row))
                {
                    row.DisplayName = displayName;
                    row.Category = category;
                    row.IsActive = true;
                }
                else
                {
                    db.Permissions.Add(new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        DisplayName = displayName,
                        Category = category,
                        IsActive = true,
                        Created = DateTime.UtcNow
                    });
                }
            }

            // Mark rows that no longer exist in the catalog as inactive (don't delete
            // — they may still be referenced by RolePermissions, and we want history).
            foreach (var row in existing.Where(r => !seen.Contains(r.Name) && r.IsActive))
                row.IsActive = false;

            await db.SaveChangesAsync();
        }

        private static IEnumerable<(string Name, string DisplayName, string Category)> EnumerateCatalog()
        {
            var rootType = typeof(Permissions);

            foreach (var nested in rootType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                var category = SplitCamelCase(nested.Name);
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (!field.IsLiteral || field.IsInitOnly || field.FieldType != typeof(string))
                        continue;

                    var name = (string?)field.GetRawConstantValue();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    yield return (name, SplitCamelCase(field.Name), category);
                }
            }
        }

        internal static string SplitCamelCase(string input) =>
            string.IsNullOrEmpty(input)
                ? input
                : Regex.Replace(input, "([A-Z])", " $1").Trim();

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email, string fullName, string password, string roleName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    UserName = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    Status = ApplicationUserStatus.Active,
                    IsActive = true,
                    Created = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            if (!await userManager.IsInRoleAsync(user, roleName))
                await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
