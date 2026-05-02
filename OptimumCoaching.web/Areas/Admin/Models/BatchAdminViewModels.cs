using Microsoft.AspNetCore.Mvc.Rendering;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class BatchListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ClassName { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class BatchFormViewModel
    {
        public Guid Id { get; set; }

        [Required, MaxLength(150), Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50), Display(Name = "Code")]
        public string? Code { get; set; }

        [MaxLength(500), Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please pick a department"), Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }

        [Display(Name = "Subject")]
        public Guid? SubjectId { get; set; }

        [Display(Name = "Teacher")]
        public Guid? TeacherId { get; set; }

        [Display(Name = "Start Date"), DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date"), DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(1, 1000), Display(Name = "Capacity")]
        public int? Capacity { get; set; }

        public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> ClassOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> SubjectOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> TeacherOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }

    public class PostBatchUpdateViewModel
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;

        [Required, MaxLength(200), Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000), Display(Name = "Message")]
        public string Body { get; set; } = string.Empty;
    }
}
