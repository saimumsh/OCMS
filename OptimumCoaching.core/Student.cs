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

        // Optional formal link to a registered Guardian record.
        public Guid? GuardianId { get; set; }
        public Guardian? Guardian { get; set; }

        // Lightweight contact fallback when there is no Guardian record yet.
        [MaxLength(150), Display(Name = "Parent / guardian name")]
        public string? GuardianName { get; set; }

        [MaxLength(30), Phone, Display(Name = "Parent / guardian phone")]
        public string? GuardianPhone { get; set; }

        [Display(Name = "Enrollment date")]
        public DateTime? EnrollmentDate { get; set; }

        // Department the student is enrolling into (required from self-registration onwards).
        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Batch assignment — typically set by admin/CC after approval.
        [Display(Name = "Batch")]
        public Guid? BatchId { get; set; }
        public Batch? Batch { get; set; }

        // Free-form list of academic records (SSC, HSC, Diploma semesters, …).
        // Students can add as many as they need.
        public IList<StudentAcademicRecord> AcademicRecords { get; set; } = new List<StudentAcademicRecord>();

        [MaxLength(500), Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Approval workflow.
        public StudentApprovalStatus ApprovalStatus { get; set; } = StudentApprovalStatus.Pending;
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }
    }
}
