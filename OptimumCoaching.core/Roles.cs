namespace OptimumCoaching.core
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Dev = "Dev";
        public const string Admin = "Admin";
        public const string CC = "CC"; // Course Coordinator — runs batches and posts class updates.
        public const string EC = "EC"; // Exam Controller — handles exams and reachable via Messages.
        public const string Finance = "Finance"; // Finance/Accounts — records fee payments and salary payouts.
        public const string Teacher = "Teacher";
        public const string User = "User";

        public static readonly string[] All = { SuperAdmin, Dev, Admin, CC, EC, Finance, Teacher, User };

        // Roles a Student/Teacher can address from the Messages feature.
        public static readonly string[] MessageableRoles = { CC, EC, Finance };

        // Roles that get unrestricted access regardless of permissions.
        public const string FullAccessRoles = SuperAdmin + "," + Dev;
    }
}
