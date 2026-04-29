using Microsoft.AspNetCore.Authorization;

namespace OptimumCoaching.web.Models.IdentityModels
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
