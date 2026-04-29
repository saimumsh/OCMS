using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Subject : AuditableEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }
    }
}
