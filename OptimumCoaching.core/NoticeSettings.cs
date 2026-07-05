using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Global, single-row defaults for the Notices module. Used when a notice
    // is created without an explicit value for that field.
    public class NoticeSettings : AuditableEntity
    {
        [Display(Name = "Default audience")]
        public NoticeAudience DefaultAudience { get; set; } = NoticeAudience.Both;

        [Range(0, 365), Display(Name = "Default expiry days")]
        public int DefaultExpiryDays { get; set; } = 30;

        [Display(Name = "Pin new notices by default")]
        public bool DefaultPinned { get; set; }

        // Fee-overdue auto-notice tuning.
        [Display(Name = "Pin overdue alerts")]
        public bool OverdueAlertPinned { get; set; } = true;

        [Range(1, 365), Display(Name = "Overdue alert expiry days")]
        public int OverdueAlertExpiryDays { get; set; } = 14;
    }

    // Reusable Title + Body template admins can pick from when posting a notice.
    public class NoticeTemplate : AuditableEntity
    {
        [Required, MaxLength(100), Display(Name = "Template name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Body")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Default audience")]
        public NoticeAudience DefaultAudience { get; set; } = NoticeAudience.Both;
    }
}
