using Microsoft.AspNetCore.Mvc.Rendering;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class SubjectListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public EducationStream? DepartmentStream { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public bool IsActive { get; set; }
    }

    public class SubjectFormViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }

        [Required, MaxLength(150), Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50), Display(Name = "Code")]
        public string? Code { get; set; }

        [MaxLength(500), Display(Name = "Description")]
        public string? Description { get; set; }

        // Populated by the controller for the form dropdowns.
        public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ClassOptions { get; set; } = new List<SelectListItem>();
    }
}
