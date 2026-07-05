using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A comment posted by a student or teacher under a CourseLesson. Supports
    // a single level of replies via ParentCommentId so discussions stay
    // readable without deep threading.
    public class LessonComment : AuditableEntity
    {
        public Guid LessonId { get; set; }
        public CourseLesson Lesson { get; set; } = null!;

        public Guid AuthorUserId { get; set; }
        public ApplicationUser? AuthorUser { get; set; }

        [Required, MaxLength(2000), Display(Name = "Comment")]
        public string Body { get; set; } = string.Empty;

        // Optional — when set this comment is a reply to another.
        public Guid? ParentCommentId { get; set; }
        public LessonComment? ParentComment { get; set; }

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }
}
