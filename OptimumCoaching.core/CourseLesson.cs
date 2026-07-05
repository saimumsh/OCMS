using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A self-paced recorded lesson belonging to a Batch. Students watch them
    // outside live-class time and mark each as completed; progress shows on
    // the student dashboard and admin lesson management.
    public class CourseLesson : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000), Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Order")]
        public int SortOrder { get; set; }

        // External video URL — YouTube, Vimeo, Drive, etc.
        [MaxLength(1000), Url, Display(Name = "Video URL")]
        public string? VideoUrl { get; set; }

        // Uploaded video file (alternative to URL).
        [MaxLength(500), Display(Name = "Uploaded video")]
        public string? FilePath { get; set; }

        // Optional supporting resource (PDF / slides).
        [MaxLength(500), Display(Name = "Resource file")]
        public string? ResourcePath { get; set; }

        // Estimated runtime for display only — minutes.
        [Display(Name = "Duration (minutes)")]
        public int? DurationMinutes { get; set; }

        [Display(Name = "Published")]
        public bool IsPublished { get; set; } = true;

        // When set, this lesson is a recording of a specific live class held
        // on that date. Otherwise it's an evergreen / self-paced lesson.
        [Display(Name = "Recorded on")]
        public DateTime? RecordedOn { get; set; }
    }

    // Per-student progress against a single lesson. Created lazily the first
    // time a student opens the lesson or marks it completed.
    public class StudentLessonProgress : AuditableEntity
    {
        public Guid LessonId { get; set; }
        public CourseLesson Lesson { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public DateTime? FirstOpenedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public bool IsCompleted => CompletedAt.HasValue;
    }
}
