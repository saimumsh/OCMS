using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Join row letting a Batch carry multiple Teachers. The lead teacher is
    // still tracked on Batch.TeacherId (so existing queries keep working) and
    // is mirrored here with IsLead = true. Additional teachers are added with
    // a free-form Role label (Co-teacher, Substitute, Assistant, …).
    public class BatchTeacher : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        // Free-form label: "Lead", "Co-teacher", "Substitute", etc. The
        // service auto-sets "Lead" for the row that mirrors Batch.TeacherId.
        [MaxLength(40), Display(Name = "Role")]
        public string? Role { get; set; }

        public bool IsLead { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }
    }
}
