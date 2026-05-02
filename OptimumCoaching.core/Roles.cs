namespace OptimumCoaching.core
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Dev = "Dev";
        public const string Admin = "Admin";
        public const string CC = "CC"; // Course Coordinator — runs batches and posts class updates.
        public const string Teacher = "Teacher";
        public const string User = "User";

        public static readonly string[] All = { SuperAdmin, Dev, Admin, CC, Teacher, User };

        // Roles that get unrestricted access regardless of permissions.
        public const string FullAccessRoles = SuperAdmin + "," + Dev;
    }
}
