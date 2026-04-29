namespace OptimumCoaching.core
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Dev = "Dev";
        public const string Admin = "Admin";
        public const string User = "User";

        public static readonly string[] All = { SuperAdmin, Dev, Admin, User };

        // Roles that get unrestricted access regardless of permissions.
        public const string FullAccessRoles = SuperAdmin + "," + Dev;
    }
}
