using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Institution-wide announcement visible to a chosen audience (Students,
    // Teachers, or Both). Posted by Admins / SuperAdmins; Teachers can post
    // notices targeted at Students only.
    public class Notice : AuditableEntity
    {
        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Audience")]
        public NoticeAudience Audience { get; set; } = NoticeAudience.Both;

        // Optional filter — when set, only members of this department see it.
        [Display(Name = "Department (optional)")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Display(Name = "Pin to top")]
        public bool IsPinned { get; set; }

        [Display(Name = "Expires on")]
        public DateTime? ExpiresAt { get; set; }

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public Guid? PostedByUserId { get; set; }
        public ApplicationUser? PostedByUser { get; set; }
    }
}
