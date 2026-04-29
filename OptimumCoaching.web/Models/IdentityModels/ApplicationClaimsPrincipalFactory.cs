using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OptimumCoaching.core;
using OptimumCoaching.service;
using System.Security.Claims;

namespace OptimumCoaching.web.Models.IdentityModels
{
    public class ApplicationClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        private readonly IPermissionService _permissionService;

        public ApplicationClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            IPermissionService permissionService)
            : base(userManager, roleManager, optionsAccessor)
        {
            _permissionService = permissionService;
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;

            identity.AddClaims(new[]
            {
                new Claim(CustomClaimType.FullName, user.FullName ?? string.Empty),
                new Claim(CustomClaimType.ImageUrl, user.ImageUrl ?? string.Empty)
            });

            var roles = await UserManager.GetRolesAsync(user);

            // Dev/SuperAdmin → universal access via wildcard permission
            if (roles.Contains(Roles.Dev) || roles.Contains(Roles.SuperAdmin))
            {
                identity.AddClaim(new Claim(CustomClaimType.Permission, "*"));
                identity.AddClaim(new Claim(CustomClaimType.Permission, Permissions.SuperAdmin));
                return principal;
            }

            var permissionNames = await _permissionService.GetPermissionNamesForUserAsync(user.Id);
            foreach (var perm in permissionNames.Distinct())
                identity.AddClaim(new Claim(CustomClaimType.Permission, perm));

            return principal;
        }
    }
}
