namespace OptimumCoaching.core
{
    public static class Permissions
    {
        public const string SuperAdmin = "Permissions.SuperAdmin";

        public static class Users
        {
            public const string ListView = "Permissions.Users.ListView";
            public const string AddEdit = "Permissions.Users.AddEdit";
            public const string Delete = "Permissions.Users.Delete";
            public const string ResetPassword = "Permissions.Users.ResetPassword";
            public const string ActiveInactive = "Permissions.Users.ActiveInactive";
        }

        public static class UserRoles
        {
            public const string ListView = "Permissions.UserRoles.ListView";
            public const string AddEdit = "Permissions.UserRoles.AddEdit";
            public const string Delete = "Permissions.UserRoles.Delete";
            public const string ActiveInactive = "Permissions.UserRoles.ActiveInactive";
            public const string ManagePermissions = "Permissions.UserRoles.ManagePermissions";
        }

        public static class PermissionCatalog
        {
            public const string ListView = "Permissions.PermissionCatalog.ListView";
            public const string AddEdit = "Permissions.PermissionCatalog.AddEdit";
            public const string Delete = "Permissions.PermissionCatalog.Delete";
        }

        public static class Dashboard
        {
            public const string View = "Permissions.Dashboard.View";
        }

        public static class Teachers
        {
            public const string ListView = "Permissions.Teachers.ListView";
            public const string AddEdit = "Permissions.Teachers.AddEdit";
            public const string Delete = "Permissions.Teachers.Delete";
            public const string ActiveInactive = "Permissions.Teachers.ActiveInactive";
        }

        public static class Students
        {
            public const string ListView = "Permissions.Students.ListView";
            public const string AddEdit = "Permissions.Students.AddEdit";
            public const string Delete = "Permissions.Students.Delete";
            public const string Approve = "Permissions.Students.Approve";
        }

        public static class Guardians
        {
            public const string ListView = "Permissions.Guardians.ListView";
            public const string AddEdit = "Permissions.Guardians.AddEdit";
            public const string Delete = "Permissions.Guardians.Delete";
        }

        public static class Departments
        {
            public const string ListView = "Permissions.Departments.ListView";
            public const string AddEdit = "Permissions.Departments.AddEdit";
            public const string Delete = "Permissions.Departments.Delete";
        }

        public static class Batches
        {
            public const string ListView = "Permissions.Batches.ListView";
            public const string AddEdit = "Permissions.Batches.AddEdit";
            public const string Delete = "Permissions.Batches.Delete";
            public const string AssignStudents = "Permissions.Batches.AssignStudents";
        }

        public static class BatchUpdates
        {
            public const string ListView = "Permissions.BatchUpdates.ListView";
            public const string Post = "Permissions.BatchUpdates.Post";
            public const string Delete = "Permissions.BatchUpdates.Delete";
        }
    }
}
