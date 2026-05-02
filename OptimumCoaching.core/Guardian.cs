using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Guardian : AuditableEntity
    {
        // Link to the login account that self-registered as a guardian.
        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(200), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150), EmailAddress]
        public string? Email { get; set; }

        [Required, MaxLength(30), Phone, Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(80), Display(Name = "Relationship")]
        public string? Relationship { get; set; }

        [MaxLength(150), Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(500), Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
