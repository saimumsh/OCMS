using Microsoft.AspNetCore.Mvc.Rendering;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Models
{
    // Friendly labels for the recipient role pills shown to senders.
    public static class RoleLabels
    {
        public static string For(string roleName) => roleName switch
        {
            Roles.CC => "Course Coordinator",
            Roles.EC => "Exam Controller",
            _        => roleName
        };
    }

    public class ComposeMessageViewModel
    {
        [Required, MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        [Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Send to")]
        public string TargetRole { get; set; } = Roles.CC;

        public IEnumerable<SelectListItem> RoleOptions { get; set; } =
            Roles.MessageableRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = RoleLabels.For(r)
            });
    }

    public class ReplyMessageViewModel
    {
        [Required]
        public Guid ConversationId { get; set; }

        [Required, MaxLength(4000)]
        [Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;
    }
}
