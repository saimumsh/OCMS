using OptimumCoaching.core.Core;
using OptimumCoaching.core.Enum;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Teacher : AuditableEntity
    {
        // Optional link to a login account. Null when the admin pre-creates a
        // teacher record before the user signs up.
        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(200), Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150), EmailAddress]
        public string? Email { get; set; }

        [MaxLength(30), Phone, Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        public Gender Gender { get; set; } = Gender.Unspecified;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        [MaxLength(200)]
        public string? Qualification { get; set; }

        [Display(Name = "Years of experience")]
        public int? ExperienceYears { get; set; }

        [Display(Name = "Hire date")]
        public DateTime? HireDate { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }

        [MaxLength(500), Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
    }
}
