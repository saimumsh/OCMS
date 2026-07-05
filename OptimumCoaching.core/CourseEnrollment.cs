using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public enum EnrollmentStatus
    {
        Active = 0,
        Cancelled = 1,
        Completed = 2,
        Suspended = 3
    }

    // Many-to-many link between a Student and an online/hybrid Batch.
    // Offline batches continue to use the single Student.BatchId field.
    // One Student can hold many of these; each enrollment owns its own
    // StudentFeeAccount (per Student+Batch).
    public class CourseEnrollment : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

        // Snapshot of the price the student paid (after any active offer) at
        // the moment of enrollment, for audit + receipt display.
        [Display(Name = "Price at enrollment")]
        public decimal PriceAtEnrollment { get; set; }

        // Optional cancellation/notes from staff.
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
