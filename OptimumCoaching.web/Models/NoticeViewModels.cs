using Microsoft.AspNetCore.Mvc.Rendering;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Models
{
    public class NoticeListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NoticeAudience Audience { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsPinned { get; set; }
        public bool IsActive { get; set; }
        public DateTime PostedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? PostedByName { get; set; }
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    }

    public class NoticeFormViewModel
    {
        public Guid Id { get; set; }

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        [Required, Display(Name = "Audience")]
        public NoticeAudience Audience { get; set; } = NoticeAudience.Both;

        [Display(Name = "Department (optional)")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Pin to top")]
        public bool IsPinned { get; set; }

        [Display(Name = "Expires on (optional)")]
        [DataType(DataType.Date)]
        public DateTime? ExpiresAt { get; set; }

        public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();

        // Set by the controller — restricts the audience options for non-admin
        // posters (Teachers can only post to Students).
        public bool RestrictAudienceToStudents { get; set; }
    }
}
