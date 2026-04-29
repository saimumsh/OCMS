using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class PermissionListRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreatePermissionViewModel
    {
        [Required, MaxLength(150)]
        [RegularExpression(@"^[A-Za-z0-9_.\-]+$",
            ErrorMessage = "Name may contain letters, numbers, dot, dash and underscore only")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200), Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class EditPermissionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200), Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
