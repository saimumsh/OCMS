using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace OptimumCoaching.web.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            Guid.TryParse(
                httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier),
                out Guid value);
            UserId = value;
            IsAuthenticated = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            Name = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;
        }

        public Guid UserId { get; }
        public string Name { get; }
        public bool IsAuthenticated { get; }
    }
}
