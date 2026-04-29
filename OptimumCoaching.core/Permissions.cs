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
    }
}
