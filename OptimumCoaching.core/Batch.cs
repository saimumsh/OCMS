using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Batch : AuditableEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }

        [Display(Name = "Subject")]
        public Guid? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Display(Name = "Teacher")]
        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Capacity")]
        public int? Capacity { get; set; }

        // ---- Fees ---------------------------------------------------
        // Headline course fee for this batch. The actual amount each student
        // owes is stored on StudentFeeAccount (after any discounts).
        [Display(Name = "Course fee")]
        public decimal CourseFee { get; set; }

        // Minimum a student must pay to be considered "enrolled" (controls
        // exam-admit-card eligibility, etc.).
        [Display(Name = "Minimum to enroll")]
        public decimal MinimumEnrollment { get; set; }

        // Discount % applied automatically when a student clears the entire
        // course fee in a single payment (0–100).
        [Display(Name = "Full-payment discount %")]
        public decimal FullPaymentDiscountPercent { get; set; }
    }
}
