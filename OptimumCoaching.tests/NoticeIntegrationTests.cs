using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.tests.Infrastructure;
using System.Net;

namespace OptimumCoaching.tests;

public class NoticeIntegrationTests : IClassFixture<OcmsWebApplicationFactory>
{
    private readonly OcmsWebApplicationFactory _factory;
    private readonly WebApplicationFactoryClientOptions _noRedirect = new() { AllowAutoRedirect = false };

    public NoticeIntegrationTests(OcmsWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_GetIndex_RedirectsToLogin()
    {
        var client = _factory.CreateClient(_noRedirect).AsAnonymous();

        var response = await client.GetAsync("/Notices");

        // [Authorize] without an authenticated user → cookie scheme would
        // redirect to /Identity/Account/Login. Our test scheme has no
        // challenge handler, so ASP.NET falls back to the configured login
        // path — assert we did NOT get 200 and are bounced away.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_GetIndex_ReturnsOk()
    {
        var client = _factory.CreateClient().AsAdmin();

        var response = await client.GetAsync("/Notices");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Notices"); // page renders
    }

    [Fact]
    public async Task Admin_PostCreate_PersistsNotice()
    {
        var client = _factory.CreateClient(_noRedirect).AsAdmin();

        var response = await client.PostFormAsync("/Notices/Create", new Dictionary<string, string?>
        {
            ["Title"] = "Mid-term exam schedule",
            ["Body"] = "All batches: mid-term starts on the 20th.",
            ["Audience"] = ((int)NoticeAudience.Both).ToString(),
            ["IsPinned"] = "true",
            // ExpiresAt + DepartmentId left empty (optional)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found, "Create should redirect to Index on success");
        response.Headers.Location!.OriginalString.Should().Contain("/Notices");

        await _factory.WithDbAsync(async db =>
        {
            var saved = await db.Notices.SingleOrDefaultAsync(n => n.Title == "Mid-term exam schedule");
            saved.Should().NotBeNull();
            saved!.Audience.Should().Be(NoticeAudience.Both);
            saved.IsPinned.Should().BeTrue();
            saved.IsDeleted.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Teacher_PostCreate_AudienceCoercedToStudents()
    {
        var client = _factory.CreateClient(_noRedirect).AsTeacher();

        // Teacher tries to broadcast to "Both" — server should pin the audience to Students.
        var response = await client.PostFormAsync("/Notices/Create", new Dictionary<string, string?>
        {
            ["Title"] = "Sneaky teacher broadcast",
            ["Body"] = "Trying to reach teachers too.",
            ["Audience"] = ((int)NoticeAudience.Both).ToString(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        await _factory.WithDbAsync(async db =>
        {
            var saved = await db.Notices.SingleOrDefaultAsync(n => n.Title == "Sneaky teacher broadcast");
            saved.Should().NotBeNull();
            saved!.Audience.Should().Be(NoticeAudience.Students,
                "EnforceTeacherAudience should pin Teacher-posted notices to Students");
        });
    }

    [Fact]
    public async Task Admin_PostCreate_PastExpiry_ReturnsValidationError()
    {
        var client = _factory.CreateClient(_noRedirect).AsAdmin();

        var response = await client.PostFormAsync("/Notices/Create", new Dictionary<string, string?>
        {
            ["Title"] = "Past expiry notice",
            ["Body"] = "Should be rejected.",
            ["Audience"] = ((int)NoticeAudience.Students).ToString(),
            ["ExpiresAt"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
        });

        // On validation failure the controller re-renders the form (200 OK)
        // rather than redirecting.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var any = await db.Notices.AnyAsync(n => n.Title == "Past expiry notice");
            any.Should().BeFalse("expired notices must not reach the database");
        });
    }

    [Fact]
    public async Task User_WithoutPermission_GetIndex_IsDenied()
    {
        // A vanilla User with no Notices.* permissions.
        var client = _factory.CreateClient(_noRedirect).AsUser(
            roles: new[] { Roles.User },
            permissions: Array.Empty<string>());

        var response = await client.GetAsync("/Notices");

        // PermissionAuthorizationHandler fails the policy → ASP.NET redirects
        // to AccessDenied (or returns 403 for API). Either is acceptable.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }
}
