using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Student : AuditableEntity
    {
        // Optional link to a login account. Null when the admin pre-creates a
        // student record before the user signs up.
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

        [MaxLength(150), Display(Name = "Parent / guardian name")]
        public string? GuardianName { get; set; }

        [MaxLength(30), Phone, Display(Name = "Parent / guardian phone")]
        public string? GuardianPhone { get; set; }

        [Display(Name = "Enrollment date")]
        public DateTime? EnrollmentDate { get; set; }

        [MaxLength(500), Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
