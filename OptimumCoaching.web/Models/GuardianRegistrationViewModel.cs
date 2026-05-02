using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Models
{
    public class GuardianRegistrationViewModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, Phone, Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress, Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Relationship")]
        public string? Relationship { get; set; }

        [Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Image { get; set; }
    }
}
