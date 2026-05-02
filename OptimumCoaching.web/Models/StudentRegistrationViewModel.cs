using Microsoft.AspNetCore.Http;
using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Models
{
    public class StudentRegistrationViewModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress, Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone, Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth"), DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public Gender Gender { get; set; } = Gender.Unspecified;

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Parent / Guardian Name")]
        public string? GuardianName { get; set; }

        [Phone, Display(Name = "Parent / Guardian Phone")]
        public string? GuardianPhone { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Image { get; set; }

        [Required(ErrorMessage = "Please select a department"), Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        // Variable-length list of academic records (SSC, HSC, Diploma semesters, etc.)
        public List<AcademicRecordInput> AcademicRecords { get; set; } = new();

        // Populated server-side for the dropdown
        public IList<DepartmentOption> Departments { get; set; } = new List<DepartmentOption>();
    }

    public class DepartmentOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EducationStream Stream { get; set; }
    }

    public class AcademicRecordInput
    {
        public Guid Id { get; set; }

        [Required, MaxLength(120), Display(Name = "Examination / Level")]
        public string ExaminationName { get; set; } = string.Empty;

        [Required, Range(1950, 2100), Display(Name = "Year")]
        public int? PassingYear { get; set; }

        [MaxLength(80), Display(Name = "Group / Department")]
        public string? Group { get; set; }

        [MaxLength(50), Display(Name = "Result")]
        public string? Result { get; set; }

        [MaxLength(200), Display(Name = "Institution / Board")]
        public string? Institution { get; set; }
    }
}
