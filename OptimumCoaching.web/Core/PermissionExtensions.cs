using OptimumCoaching.core;
using System.Security.Claims;

namespace OptimumCoaching.web.Core
{
    public static class PermissionExtensions
    {
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;

            // Wildcard / explicit SuperAdmin claim grants any permission.
            if (user.HasClaim(CustomClaimType.Permission, "*")) return true;
            if (user.HasClaim(CustomClaimType.Permission, Permissions.SuperAdmin)) return true;

            return user.HasClaim(CustomClaimType.Permission, permission);
        }
    }
}
