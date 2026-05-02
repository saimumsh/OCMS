using Microsoft.AspNetCore.Http;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class TeacherListItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? HireDate { get; set; }
    }

    public class TeacherFormViewModel
    {
        public Guid Id { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress, Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone, Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth"), DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public Gender Gender { get; set; } = Gender.Unspecified;

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }

        [Display(Name = "Qualification")]
        public string? Qualification { get; set; }

        [Display(Name = "Years of Experience"), Range(0, 80)]
        public int? ExperienceYears { get; set; }

        [Display(Name = "Hire Date"), DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool RemoveImage { get; set; }
    }
}
