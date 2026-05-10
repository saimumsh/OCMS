using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A formal complaint a Student raises against a Teacher. Admin-only
    // visibility with a status workflow (Open → Investigating → Resolved).
    public class TeacherReport : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public Guid? BatchId { get; set; }
        public Batch? Batch { get; set; }

        [Display(Name = "Category")]
        public ReportCategory Category { get; set; } = ReportCategory.Other;

        [Required, MaxLength(2000), Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        public ReportStatus Status { get; set; } = ReportStatus.Open;

        [MaxLength(2000), Display(Name = "Admin note")]
        public string? AdminNote { get; set; }

        public Guid? HandledByUserId { get; set; }
        public ApplicationUser? HandledByUser { get; set; }
        public DateTime? HandledAt { get; set; }
    }
}
