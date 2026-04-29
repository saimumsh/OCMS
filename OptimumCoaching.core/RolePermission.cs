namespace OptimumCoaching.core
{
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public ApplicationRole Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
    }
}
