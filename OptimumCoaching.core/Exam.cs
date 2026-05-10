using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // An exam belongs to a Batch — only that batch's enrolled students are
    // graded for it. Lifecycle: Draft → results entered → Published.
    public class Exam : AuditableEntity
    {
        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        [Display(Name = "Type")]
        public ExamType Type { get; set; } = ExamType.Quiz;

        public ExamStatus Status { get; set; } = ExamStatus.Draft;

        [Display(Name = "Exam date")]
        public DateTime ExamDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Total marks")]
        [Range(0, 1000)]
        public decimal TotalMarks { get; set; } = 100m;

        [Display(Name = "Pass marks")]
        [Range(0, 1000)]
        public decimal PassMarks { get; set; } = 40m;

        [Display(Name = "Duration (minutes)")]
        public int? DurationMinutes { get; set; }

        [MaxLength(2000), Display(Name = "Syllabus / instructions")]
        public string? Syllabus { get; set; }

        public IList<ExamResult> Results { get; set; } = new List<ExamResult>();
    }

    // Per-student exam result. Marks are nullable so absentees can be recorded.
    public class ExamResult : AuditableEntity
    {
        public Guid ExamId { get; set; }
        public Exam Exam { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Display(Name = "Present")]
        public bool IsPresent { get; set; } = true;

        [Range(0, 1000), Display(Name = "Marks obtained")]
        public decimal? MarksObtained { get; set; }

        [MaxLength(500), Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        public ResultStatus Status { get; set; } = ResultStatus.Draft;

        public DateTime? PublishedAt { get; set; }
        public Guid? PublishedByUserId { get; set; }
    }
}
