using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Batch : AuditableEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }

        [Display(Name = "Subject")]
        public Guid? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Display(Name = "Teacher")]
        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Capacity")]
        public int? Capacity { get; set; }
    }
}
