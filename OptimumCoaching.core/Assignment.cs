using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A teacher-assigned piece of work attached to a Batch. Students see it
    // on their dashboard, submit a file or text reply, and the teacher grades.
    public class Assignment : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Instructions")]
        public string Instructions { get; set; } = string.Empty;

        [Display(Name = "Due")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Max score")]
        public decimal MaxScore { get; set; } = 100m;

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

        // Optional reference material for the assignment (PDF question paper).
        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        public IList<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }

    // One row per student per assignment. Created when the student submits.
    public class AssignmentSubmission : AuditableEntity
    {
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        // Either a written response or an uploaded file (or both).
        [MaxLength(8000), Display(Name = "Response")]
        public string? ResponseText { get; set; }

        [MaxLength(500), Display(Name = "File")]
        public string? FilePath { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        public decimal? Score { get; set; }

        [MaxLength(2000), Display(Name = "Feedback")]
        public string? Feedback { get; set; }

        public Guid? GradedByUserId { get; set; }
        public ApplicationUser? GradedByUser { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
