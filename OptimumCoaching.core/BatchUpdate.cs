using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Short class announcement / class update visible on the Batch students'
    // dashboards. Posted by Teachers / CC / Admins.
    public class BatchUpdate : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Body")]
        public string Body { get; set; } = string.Empty;

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public Guid? PostedByUserId { get; set; }
        public ApplicationUser? PostedByUser { get; set; }
    }
}
