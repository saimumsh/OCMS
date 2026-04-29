using Microsoft.AspNetCore.Identity;

namespace OptimumCoaching.core
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRoleStatus Status { get; set; }

        public Guid? CreatedBy { get; set; }
        public DateTime Created { get; set; }
        public Guid? LastModifiedBy { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public IList<ApplicationUserRole> UserRoles { get; set; }
        public IList<RolePermission> RolePermissions { get; set; }

        public ApplicationRole() : base()
        {
            this.IsActive = true;
            this.IsDeleted = false;
            this.Status = ApplicationRoleStatus.Active;
            this.Created = DateTime.UtcNow;
            this.UserRoles = new List<ApplicationUserRole>();
            this.RolePermissions = new List<RolePermission>();
        }

        public ApplicationRole(string roleName) : base(roleName)
        {
            this.IsActive = true;
            this.IsDeleted = false;
            this.Status = ApplicationRoleStatus.Active;
            this.Created = DateTime.UtcNow;
            this.UserRoles = new List<ApplicationUserRole>();
            this.RolePermissions = new List<RolePermission>();
        }
    }
}
