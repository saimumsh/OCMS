using Microsoft.AspNetCore.Authorization;
using OptimumCoaching.core;

namespace OptimumCoaching.web.Models.IdentityModels
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            // SuperAdmin / Dev are granted the wildcard permission claim by the
            // ApplicationClaimsPrincipalFactory, plus the explicit SuperAdmin
            // permission. Either grants access to any policy.
            if (context.User.HasClaim(CustomClaimType.Permission, "*") ||
                context.User.HasClaim(CustomClaimType.Permission, Permissions.SuperAdmin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (context.User.HasClaim(CustomClaimType.Permission, requirement.Permission))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
