using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A single class session whose attendance has been taken. Created
    // explicitly when a teacher opens "Mark attendance" for a batch on a
    // particular date. Unique per (Batch, SessionDate).
    public class AttendanceSession : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        public Guid? TakenByUserId { get; set; }
        public ApplicationUser? TakenByUser { get; set; }

        [Display(Name = "Session date")]
        public DateTime SessionDate { get; set; } = DateTime.UtcNow.Date;

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }

        public IList<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }

    // Per-student attendance row for a session.
    public class AttendanceRecord : AuditableEntity
    {
        public Guid SessionId { get; set; }
        public AttendanceSession Session { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [MaxLength(200), Display(Name = "Remarks")]
        public string? Remarks { get; set; }
    }
}
