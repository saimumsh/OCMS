using OptimumCoaching.core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class UserListItem
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ApplicationUserStatus Status { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserViewModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(5)]
        public string Password { get; set; } = string.Empty;

        public List<string> SelectedRoles { get; set; } = new();
        public IList<string> AllRoles { get; set; } = new List<string>();
    }

    public class EditUserViewModel
    {
        public Guid Id { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public ApplicationUserStatus Status { get; set; }

        public List<string> SelectedRoles { get; set; } = new();
        public IList<string> AllRoles { get; set; } = new List<string>();
    }

    public class ResetUserPasswordViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(5)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
