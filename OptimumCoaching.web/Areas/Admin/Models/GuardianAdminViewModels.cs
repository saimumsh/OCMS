using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class GuardianListItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Relationship { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
    }

    public class GuardianFormViewModel
    {
        public Guid Id { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress, Display(Name = "Email")]
        public string? Email { get; set; }

        [Required, Phone, Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Relationship")]
        public string? Relationship { get; set; }

        [Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool RemoveImage { get; set; }
    }
}
