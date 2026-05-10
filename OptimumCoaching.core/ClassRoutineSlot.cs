using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A weekly recurring class slot for a Batch. Together these rows make up
    // the standing weekly routine. Individual sessions can be overridden via
    // ClassSessionOverride for cancellations / reschedules.
    public class ClassRoutineSlot : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        // Optional — when the batch covers multiple topics with different
        // teachers, a slot can be tied to a specific topic.
        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Display(Name = "Day")]
        public DayOfWeek Day { get; set; } = DayOfWeek.Monday;

        // Stored as ticks-of-day so EF maps to TimeSpan cleanly.
        [Display(Name = "Start time")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(18, 0, 0);

        [Display(Name = "End time")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(19, 30, 0);

        [MaxLength(120), Display(Name = "Room / location")]
        public string? Room { get; set; }
    }

    // A one-off override for a specific calendar date — cancels or moves the
    // session that would otherwise run from a ClassRoutineSlot. May also stand
    // alone (extra session not in the weekly routine).
    public class ClassSessionOverride : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        // When the override modifies a recurring slot, points back to it.
        // Null means this is an ad-hoc extra session.
        public Guid? RoutineSlotId { get; set; }
        public ClassRoutineSlot? RoutineSlot { get; set; }

        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Display(Name = "Date")]
        public DateTime SessionDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Start time")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End time")]
        public TimeSpan? EndTime { get; set; }

        [MaxLength(120), Display(Name = "Room / location")]
        public string? Room { get; set; }

        [Display(Name = "Cancelled")]
        public bool IsCancelled { get; set; }

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }
    }
}
