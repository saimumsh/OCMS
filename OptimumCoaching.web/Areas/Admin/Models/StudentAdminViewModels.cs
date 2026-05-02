using Microsoft.AspNetCore.Http;
using OptimumCoaching.core;
using OptimumCoaching.web.Models;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class StudentListItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public StudentApprovalStatus ApprovalStatus { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime Created { get; set; }
        public string? GuardianName { get; set; }
    }

    public class StudentFormViewModel
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

        public Gender Gender { get; set; } = Gender.Unspecified;

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Parent / Guardian Name")]
        public string? GuardianName { get; set; }

        [Phone, Display(Name = "Parent / Guardian Phone")]
        public string? GuardianPhone { get; set; }

        [Display(Name = "Enrollment Date"), DataType(DataType.Date)]
        public DateTime? EnrollmentDate { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please select a department"), Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Batch")]
        public Guid? BatchId { get; set; }

        public List<AcademicRecordInput> AcademicRecords { get; set; } = new();

        public IList<BatchOption> BatchOptions { get; set; } = new List<BatchOption>();

        [Display(Name = "Profile Picture")]
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool RemoveImage { get; set; }

        public StudentApprovalStatus ApprovalStatus { get; set; } = StudentApprovalStatus.Approved;

        public IList<DepartmentOption> Departments { get; set; } = new List<DepartmentOption>();
    }

    public class BatchOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
    }

    public class RejectStudentViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Reason"), MaxLength(500)]
        public string? Reason { get; set; }
    }
}
