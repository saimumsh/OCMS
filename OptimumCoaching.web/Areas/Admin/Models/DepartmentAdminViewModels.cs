using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class DepartmentListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public EducationStream Stream { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentFormViewModel
    {
        public Guid Id { get; set; }

        [Required, Display(Name = "Stream")]
        public EducationStream Stream { get; set; } = EducationStream.Academic;

        [Required, MaxLength(150), Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50), Display(Name = "Code")]
        public string? Code { get; set; }

        [MaxLength(500), Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
