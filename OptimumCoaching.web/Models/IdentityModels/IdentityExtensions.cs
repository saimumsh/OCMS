using OptimumCoaching.core;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace OptimumCoaching.web.Models.IdentityModels
{
    public static class IdentityExtensions
    {
        public static string Id(this IIdentity identity)
        {
            var value = ((ClaimsIdentity?)identity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return value ?? string.Empty;
        }

        public static string UserName(this IIdentity identity)
        {
            var value = ((ClaimsIdentity?)identity)?.FindFirst(ClaimTypes.Name)?.Value;
            return value ?? string.Empty;
        }

        public static string FullName(this IIdentity identity)
        {
            var value = ((ClaimsIdentity?)identity)?.FindFirst(CustomClaimType.FullName)?.Value;
            return value ?? string.Empty;
        }

        public static string ImageUrl(this IIdentity identity)
        {
            var value = ((ClaimsIdentity?)identity)?.FindFirst(CustomClaimType.ImageUrl)?.Value;
            return value ?? string.Empty;
        }

        public static string RoleName(this IIdentity identity)
        {
            var value = ((ClaimsIdentity?)identity)?.FindFirst(ClaimTypes.Role)?.Value;
            return value ?? string.Empty;
        }

        public static bool HasRole(this IIdentity identity, string role)
        {
            var hasClaim = ((ClaimsIdentity?)identity)?.HasClaim(x => x.Type == ClaimTypes.Role && x.Value == role);
            return hasClaim ?? false;
        }

        // All role names carried by this principal — read from claims so we
        // never have to hit the user store from a view/partial.
        public static IList<string> GetRoles(this IIdentity identity)
        {
            var ci = (ClaimsIdentity?)identity;
            if (ci == null) return new List<string>();
            return ci.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        public static bool HasPermission(this IIdentity identity, string permission)
        {
            var ci = (ClaimsIdentity?)identity;
            if (ci == null) return false;
            // Mirror PermissionAuthorizationHandler: SuperAdmin / Dev get a
            // wildcard claim that satisfies every permission policy. Honour
            // it here too so UI gates match server-side authorization.
            if (ci.HasClaim(CustomClaimType.Permission, "*") ||
                ci.HasClaim(CustomClaimType.Permission, Permissions.SuperAdmin))
                return true;
            return ci.HasClaim(x => x.Type == CustomClaimType.Permission && x.Value == permission);
        }

        public static bool HasPermission(this IIdentity identity, params string[] permissions)
        {
            var ci = (ClaimsIdentity?)identity;
            if (ci == null) return false;
            if (ci.HasClaim(CustomClaimType.Permission, "*") ||
                ci.HasClaim(CustomClaimType.Permission, Permissions.SuperAdmin))
                return true;
            return ci.Claims.Any(cl =>
                cl.Type == CustomClaimType.Permission && permissions.Any(per => per == cl.Value));
        }
    }
}
