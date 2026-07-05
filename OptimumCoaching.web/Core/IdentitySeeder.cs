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
            // Production uses SQL Server (relational) and migrations. Tests
            // swap in the EF InMemory provider, which doesn't support
            // migrations — fall back to EnsureCreated there.
            if (db.Database.IsRelational())
                await db.Database.MigrateAsync();
            else
                await db.Database.EnsureCreatedAsync();

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

            // 2b) Default Admin role grants — Teachers + Students + Guardians management
            await EnsureRolePermissionsAsync(db, roleManager, Roles.Admin, new[]
            {
                Permissions.Teachers.ListView,
                Permissions.Teachers.AddEdit,
                Permissions.Teachers.Delete,
                Permissions.Teachers.ActiveInactive,
                Permissions.Students.ListView,
                Permissions.Students.AddEdit,
                Permissions.Students.Delete,
                Permissions.Students.Approve,
                Permissions.Guardians.ListView,
                Permissions.Guardians.AddEdit,
                Permissions.Guardians.Delete,
                Permissions.Departments.ListView,
                Permissions.Departments.AddEdit,
                Permissions.Departments.Delete,
                Permissions.Subjects.ListView,
                Permissions.Subjects.AddEdit,
                Permissions.Subjects.Delete,
                Permissions.Batches.ListView,
                Permissions.Batches.AddEdit,
                Permissions.Batches.Delete,
                Permissions.Batches.AssignStudents,
                Permissions.OnlineCourses.ListView,
                Permissions.OnlineCourses.AddEdit,
                Permissions.OnlineCourses.Delete,
                Permissions.OnlineCourses.ManageEnrollments,
                Permissions.BatchUpdates.ListView,
                Permissions.BatchUpdates.Post,
                Permissions.BatchUpdates.Delete,
                Permissions.Notices.ListView,
                Permissions.Notices.AddEdit,
                Permissions.Notices.Delete,
                Permissions.Exams.ListView,
                Permissions.Exams.AddEdit,
                Permissions.Exams.Delete,
                Permissions.Exams.Publish,
                Permissions.Results.Grade,
                Permissions.Results.Publish,
                Permissions.TeacherReports.ListView,
                Permissions.TeacherReports.Handle,
                Permissions.Topics.ListView,
                Permissions.Topics.AddEdit,
                Permissions.Topics.Delete,
                Permissions.Topics.AssignTeachers,
                Permissions.Materials.ListView,
                Permissions.Materials.Upload,
                Permissions.Materials.Delete,
                Permissions.Routine.ListView,
                Permissions.Routine.AddEdit,
                Permissions.Routine.Delete,
                Permissions.Lessons.ListView,
                Permissions.Lessons.Manage,
                Permissions.Assignments.ListView,
                Permissions.Assignments.Manage,
                Permissions.Assignments.Grade,
                Permissions.Finance.ListView,
                Permissions.Finance.ManageFees,
                Permissions.Finance.RecordPayment,
                Permissions.Finance.ViewSalaries,
                Permissions.Finance.RecordSalary,
                Permissions.Attendance.ListView,
                Permissions.Attendance.Mark,
                Permissions.Attendance.Reports,
                Permissions.Users.ListView,
            });

            // 2b-finance) Finance / Accounts role — focused on payments only.
            await EnsureRolePermissionsAsync(db, roleManager, Roles.Finance, new[]
            {
                Permissions.Finance.ListView,
                Permissions.Finance.ManageFees,
                Permissions.Finance.RecordPayment,
                Permissions.Finance.ViewSalaries,
                Permissions.Finance.RecordSalary,
                Permissions.Students.ListView,
                Permissions.Teachers.ListView,
                Permissions.Batches.ListView,
                Permissions.Notices.ListView,
            });

            // 2b-cc) Course Coordinator role grants — manages batches & posts updates.
            await EnsureRolePermissionsAsync(db, roleManager, Roles.CC, new[]
            {
                Permissions.Batches.ListView,
                Permissions.Batches.AddEdit,
                Permissions.Batches.AssignStudents,
                Permissions.OnlineCourses.ListView,
                Permissions.OnlineCourses.AddEdit,
                Permissions.OnlineCourses.ManageEnrollments,
                Permissions.BatchUpdates.ListView,
                Permissions.BatchUpdates.Post,
                Permissions.Students.ListView,
                Permissions.Teachers.ListView,
                Permissions.Departments.ListView,
                Permissions.Subjects.ListView,
                Permissions.Exams.ListView,
                Permissions.Topics.ListView,
                Permissions.Topics.AssignTeachers,
                Permissions.Materials.ListView,
                Permissions.Materials.Upload,
                Permissions.Routine.ListView,
                Permissions.Routine.AddEdit,
                Permissions.Attendance.ListView,
                Permissions.Attendance.Mark,
                Permissions.Attendance.Reports,
            });

            // 2b-teacher) Teacher role grants — can post class updates and exams for their batches.
            await EnsureRolePermissionsAsync(db, roleManager, Roles.Teacher, new[]
            {
                Permissions.Batches.ListView,
                Permissions.BatchUpdates.ListView,
                Permissions.BatchUpdates.Post,
                Permissions.Notices.ListView,
                Permissions.Notices.AddEdit,
                Permissions.Exams.ListView,
                Permissions.Exams.AddEdit,
                Permissions.Exams.Publish,
                Permissions.Results.Grade,
                Permissions.Results.Publish,
                Permissions.Topics.ListView,
                Permissions.Materials.ListView,
                Permissions.Materials.Upload,
                Permissions.Routine.ListView,
                Permissions.Attendance.ListView,
                Permissions.Attendance.Mark,
                Permissions.Lessons.ListView,
                Permissions.Lessons.Manage,
                Permissions.Assignments.ListView,
                Permissions.Assignments.Manage,
                Permissions.Assignments.Grade,
            });

            // 2c) Default departments per stream
            await SeedDefaultDepartmentsAsync(db);

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

        // Seeds the canonical Department list per stream. Only inserts what's
        // missing — admins may have customised the list via the UI, so we
        // never delete or rename existing rows.
        private static async Task SeedDefaultDepartmentsAsync(ApplicationDbContext db)
        {
            var defaults = new (string Name, string Code, EducationStream Stream)[]
            {
                ("Science",     "ACA-SCI", EducationStream.Academic),
                ("Arts",        "ACA-ART", EducationStream.Academic),
                ("Commerce",    "ACA-COM", EducationStream.Academic),
                ("Electrical",  "DIP-EE",  EducationStream.Diploma),
                ("Electronics", "DIP-ECE", EducationStream.Diploma),
                ("Computer",    "DIP-CSE", EducationStream.Diploma),
                ("Civil",       "DIP-CE",  EducationStream.Diploma),
                ("Mechanical",  "DIP-ME",  EducationStream.Diploma),
            };

            foreach (var (name, code, stream) in defaults)
            {
                var exists = await db.Departments.AnyAsync(d =>
                    d.Stream == stream && d.Name == name);
                if (exists) continue;

                db.Departments.Add(new Department
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Code = code,
                    Stream = stream,
                    IsActive = true,
                    IsDeleted = false,
                    Created = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        // Idempotently grants the given permissions to the role. Adds missing
        // RolePermission rows; never removes or downgrades existing grants
        // (admins may have customised them via the UI).
        private static async Task EnsureRolePermissionsAsync(
            ApplicationDbContext db,
            RoleManager<ApplicationRole> roleManager,
            string roleName,
            IEnumerable<string> permissionNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;

            var permissions = await db.Permissions
                .Where(p => permissionNames.Contains(p.Name))
                .ToListAsync();

            var existing = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            var existingSet = new HashSet<Guid>(existing);

            foreach (var p in permissions.Where(p => !existingSet.Contains(p.Id)))
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    Created = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

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
