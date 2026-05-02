using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Department : AuditableEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Whether this department belongs to the Academic (Science/Arts/Commerce)
        // or Diploma (Electrical/Computer/Civil/...) stream.
        [Display(Name = "Stream")]
        public EducationStream Stream { get; set; } = EducationStream.Academic;
    }
}
