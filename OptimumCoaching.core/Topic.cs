using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A teachable unit within a Subject. The same Subject can be split into
    // many Topics, each potentially assigned to a different Teacher per Batch.
    public class Topic : AuditableEntity
    {
        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Order")]
        public int SortOrder { get; set; }
    }

    // Pairs a Topic with the Teacher who teaches it inside a specific Batch.
    // Batch.Teacher remains the lead/primary teacher; assignments here are
    // additional and topic-scoped.
    public class BatchTopicAssignment : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid TopicId { get; set; }
        public Topic Topic { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }
    }
}
