using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;

namespace OptimumCoaching.repo
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser, ApplicationRole, Guid,
        IdentityUserClaim<Guid>, ApplicationUserRole, IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<Group> Groups => Set<Group>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(200);
            });

            builder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Name).IsUnique();
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .IsRequired();

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .IsRequired();
            });

            builder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.UserId).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(s => s.User)
                    .WithOne()
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Teacher>(entity =>
            {
                entity.HasIndex(t => t.UserId).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(t => t.User)
                    .WithOne()
                    .HasForeignKey<Teacher>(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            });

            builder.Entity<Class>(e =>
            {
                e.HasIndex(c => c.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(c => c.Department)
                    .WithMany()
                    .HasForeignKey(c => c.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Subject>(e =>
            {
                e.HasIndex(s => s.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(s => s.Department)
                    .WithMany()
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(s => s.Class)
                    .WithMany()
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Batch>(e =>
            {
                e.HasIndex(b => b.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(b => b.Class)
                    .WithMany()
                    .HasForeignKey(b => b.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(b => b.Subject)
                    .WithMany()
                    .HasForeignKey(b => b.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(b => b.Teacher)
                    .WithMany()
                    .HasForeignKey(b => b.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Group>(e =>
            {
                e.HasIndex(g => g.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(g => g.Batch)
                    .WithMany()
                    .HasForeignKey(g => g.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
