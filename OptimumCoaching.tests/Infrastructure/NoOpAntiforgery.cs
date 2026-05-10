using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace OptimumCoaching.tests.Infrastructure;

// Replaces the production IAntiforgery so test POSTs don't have to round-trip
// a hidden token. Validation always succeeds; token generation returns
// constant placeholder values.
internal sealed class NoOpAntiforgery : IAntiforgery
{
    private static readonly AntiforgeryTokenSet TokenSet =
        new("test-token", "test-cookie-token", "__RequestVerificationToken", "X-XSRF-TOKEN");

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => TokenSet;
    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => TokenSet;
    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
    public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    public void SetCookieTokenAndHeader(HttpContext httpContext) { }
}
