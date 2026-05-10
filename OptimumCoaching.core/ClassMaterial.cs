using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // A learning resource attached to a Batch. Either an external link/video
    // (URL) or an uploaded file (FilePath). Optional Topic tag for grouping
    // by chapter/unit.
    public class ClassMaterial : AuditableEntity
    {
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public Guid? TopicId { get; set; }
        public Topic? Topic { get; set; }

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000), Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Type")]
        public MaterialType Type { get; set; } = MaterialType.Document;

        // External URL — used for videos (YouTube/Vimeo/Drive) or general links.
        [MaxLength(1000), Url, Display(Name = "URL")]
        public string? Url { get; set; }

        // Path under wwwroot to an uploaded file (PDF, slides, etc.).
        [MaxLength(500), Display(Name = "File")]
        public string? FilePath { get; set; }

        public Guid? UploadedByUserId { get; set; }
        public ApplicationUser? UploadedByUser { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
