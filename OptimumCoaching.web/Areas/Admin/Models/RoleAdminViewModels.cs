using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class RoleListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }
        public int PermissionCount { get; set; }
    }

    public class CreateRoleViewModel
    {
        [Required, Display(Name = "Role name")]
        public string Name { get; set; } = string.Empty;
    }

    public class EditRoleViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsSystemRole { get; set; }
    }

    public class RolePermissionsViewModel
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Catalog-driven tree (populated from the static Permissions class).
        public RolePermissionHelper Tree { get; set; } = new();

        // Posted permission names (one per checked leaf).
        public List<string> SelectedPermissions { get; set; } = new();
    }
}
