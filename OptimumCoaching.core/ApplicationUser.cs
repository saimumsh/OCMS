using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? LastPassword { get; set; }
        public DateTime? LastPassChangeDate { get; set; }
        public int PasswordChangedCount { get; set; }
        public ApplicationUserStatus Status { get; set; }

        public Guid? CreatedBy { get; set; }
        public DateTime Created { get; set; }
        public Guid? LastModifiedBy { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public IList<ApplicationUserRole> UserRoles { get; set; }

        public ApplicationUser() : base()
        {
            this.IsActive = true;
            this.IsDeleted = false;
            this.Status = ApplicationUserStatus.Active;
            this.Created = DateTime.UtcNow;
            this.UserRoles = new List<ApplicationUserRole>();
        }
    }
}
