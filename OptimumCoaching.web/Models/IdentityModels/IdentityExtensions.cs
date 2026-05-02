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

        public static bool HasPermission(this IIdentity identity, string permission)
        {
            var hasClaim = ((ClaimsIdentity?)identity)?.HasClaim(x => x.Type == CustomClaimType.Permission && x.Value == permission);
            return hasClaim ?? false;
        }

        public static bool HasPermission(this IIdentity identity, params string[] permissions)
        {
            var hasClaim = ((ClaimsIdentity?)identity)?.Claims.Any(cl =>
                cl.Type == CustomClaimType.Permission && permissions.Any(per => per == cl.Value));
            return hasClaim ?? false;
        }
    }
}
