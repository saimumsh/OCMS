using System.Net.Http.Headers;

namespace OptimumCoaching.tests.Infrastructure;

// Helpers that decorate an HttpClient with the X-Test-* headers the
// TestAuthHandler reads. Each call returns a fresh client to keep tests
// from accidentally bleeding identity across requests.
public static class HttpClientExtensions
{
    public static HttpClient AsAdmin(this HttpClient client, params string[] extraPermissions) =>
        client.AsUser(
            roles: new[] { Roles.Admin },
            permissions: new[]
            {
                Permissions.Notices.ListView,
                Permissions.Notices.AddEdit,
                Permissions.Notices.Delete,
                Permissions.Subjects.ListView,
                Permissions.Subjects.AddEdit,
                Permissions.Subjects.Delete,
                Permissions.Departments.ListView,
                Permissions.Departments.AddEdit,
                Permissions.Departments.Delete,
            }.Concat(extraPermissions).Distinct().ToArray());

    public static HttpClient AsTeacher(this HttpClient client, params string[] extraPermissions) =>
        client.AsUser(
            roles: new[] { Roles.Teacher },
            permissions: new[]
            {
                Permissions.Notices.ListView,
                Permissions.Notices.AddEdit,
            }.Concat(extraPermissions).Distinct().ToArray());

    public static HttpClient AsAnonymous(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Remove("X-Test-UserName");
        client.DefaultRequestHeaders.Remove("X-Test-FullName");
        client.DefaultRequestHeaders.Remove("X-Test-Roles");
        client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        return client;
    }

    public static HttpClient AsUser(
        this HttpClient client,
        Guid? userId = null,
        string? userName = null,
        string? fullName = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        client.AsAnonymous();
        client.DefaultRequestHeaders.Add("X-Test-UserId", (userId ?? Guid.NewGuid()).ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserName", userName ?? "test@local");
        client.DefaultRequestHeaders.Add("X-Test-FullName", fullName ?? "Test User");
        if (roles != null) client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        if (permissions != null) client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(",", permissions));
        return client;
    }

    // Convenience for posting form data without writing the boilerplate every time.
    public static Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string url, IDictionary<string, string?> fields)
    {
        var pairs = fields
            .Where(kv => kv.Value != null)
            .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!));
        var content = new FormUrlEncodedContent(pairs);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        return client.PostAsync(url, content);
    }
}
