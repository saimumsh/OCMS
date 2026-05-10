using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A 1–5 star rating + optional comment a Student leaves about a Teacher.
    // Visible to the teacher's average rating display and to admins/CC.
    // One review per student per teacher (latest replaces).
    public class TeacherReview : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        // Optional batch context — useful when a student has been in
        // several batches under different teachers.
        public Guid? BatchId { get; set; }
        public Batch? Batch { get; set; }

        [Range(1, 5), Display(Name = "Rating")]
        public int Rating { get; set; }

        [MaxLength(2000), Display(Name = "Comment")]
        public string? Comment { get; set; }
    }
}
