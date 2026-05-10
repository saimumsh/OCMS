using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OptimumCoaching.web.Models.IdentityModels;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OptimumCoaching.tests.Infrastructure;

// Test-only auth scheme that constructs a ClaimsPrincipal from request
// headers, so each test can opt into a specific identity / role / permission
// set without dragging in cookies or password sign-in.
//
// Headers (all optional, comma-separated for multi-value):
//   X-Test-UserId       — Guid; defaults to a random anonymous user
//   X-Test-UserName     — string; defaults to "test@local"
//   X-Test-FullName     — string; defaults to "Test User"
//   X-Test-Roles        — comma-separated role names
//   X-Test-Permissions  — comma-separated permission names
//
// If no X-Test-UserId header is present, the request is treated as
// unauthenticated so [Authorize] still rejects it.
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, Request.Headers["X-Test-UserName"].FirstOrDefault() ?? "test@local"),
            new(CustomClaimType.FullName, Request.Headers["X-Test-FullName"].FirstOrDefault() ?? "Test User"),
        };

        foreach (var role in Split(Request.Headers["X-Test-Roles"].FirstOrDefault()))
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var perm in Split(Request.Headers["X-Test-Permissions"].FirstOrDefault()))
            claims.Add(new Claim(CustomClaimType.Permission, perm));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static IEnumerable<string> Split(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
