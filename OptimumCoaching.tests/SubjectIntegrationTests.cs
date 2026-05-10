using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.tests.Infrastructure;
using System.Net;

namespace OptimumCoaching.tests;

public class SubjectIntegrationTests : IClassFixture<OcmsWebApplicationFactory>
{
    private readonly OcmsWebApplicationFactory _factory;
    private readonly WebApplicationFactoryClientOptions _noRedirect = new() { AllowAutoRedirect = false };

    public SubjectIntegrationTests(OcmsWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_GetIndex_IsRejected()
    {
        var client = _factory.CreateClient(_noRedirect).AsAnonymous();

        var response = await client.GetAsync("/Admin/Subjects");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_GetIndex_ReturnsOk()
    {
        var client = _factory.CreateClient().AsAdmin();

        var response = await client.GetAsync("/Admin/Subjects");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Admin_CreateSubject_PersistsToDb()
    {
        // Seed a Department so the form has something to attach to.
        var deptId = Guid.NewGuid();
        await _factory.WithDbAsync(async db =>
        {
            db.Departments.Add(new Department
            {
                Id = deptId,
                Name = "Science",
                Code = "TST-SCI",
                Stream = EducationStream.Academic,
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateClient(_noRedirect).AsAdmin();

        var response = await client.PostFormAsync("/Admin/Subjects/Create", new Dictionary<string, string?>
        {
            ["Name"] = "Physics",
            ["Code"] = "PHY-101",
            ["DepartmentId"] = deptId.ToString(),
            ["Description"] = "Mechanics and electromagnetism",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.Should().Contain("/Admin/Subjects");

        await _factory.WithDbAsync(async db =>
        {
            var saved = await db.Subjects.SingleOrDefaultAsync(s => s.Name == "Physics");
            saved.Should().NotBeNull();
            saved!.DepartmentId.Should().Be(deptId);
            saved.Code.Should().Be("PHY-101");
        });
    }

    [Fact]
    public async Task Admin_CreateDuplicateNameInSameDepartment_RejectedByValidation()
    {
        var deptId = Guid.NewGuid();
        await _factory.WithDbAsync(async db =>
        {
            db.Departments.Add(new Department
            {
                Id = deptId, Name = "Arts", Code = "TST-ART",
                Stream = EducationStream.Academic, IsActive = true
            });
            db.Subjects.Add(new Subject
            {
                Id = Guid.NewGuid(),
                Name = "History",
                DepartmentId = deptId,
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateClient(_noRedirect).AsAdmin();

        var response = await client.PostFormAsync("/Admin/Subjects/Create", new Dictionary<string, string?>
        {
            ["Name"] = "History",
            ["DepartmentId"] = deptId.ToString(),
        });

        // Server-side dedupe in SubjectService.CreateAsync re-renders the form
        // with a ModelState error → 200 OK, not 302.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.WithDbAsync(async db =>
        {
            var count = await db.Subjects.CountAsync(s => s.Name == "History" && s.DepartmentId == deptId);
            count.Should().Be(1, "the duplicate must not have been inserted");
        });
    }

    [Fact]
    public async Task Admin_DeleteSubject_LinkedToBatch_KeepsSubject()
    {
        // Set up Subject + a Batch that references it. Delete should soft-fail.
        var deptId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        await _factory.WithDbAsync(async db =>
        {
            db.Departments.Add(new Department
            {
                Id = deptId, Name = "Commerce", Code = "TST-COM",
                Stream = EducationStream.Academic, IsActive = true
            });
            db.Subjects.Add(new Subject
            {
                Id = subjectId, Name = "Accounting",
                DepartmentId = deptId, IsActive = true
            });
            db.Batches.Add(new Batch
            {
                Id = Guid.NewGuid(),
                Name = "Acc-Morning",
                SubjectId = subjectId,
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateClient(_noRedirect).AsAdmin();

        // Delete is POST with the id in the route.
        var response = await client.PostFormAsync($"/Admin/Subjects/Delete/{subjectId}",
            new Dictionary<string, string?>());

        response.StatusCode.Should().Be(HttpStatusCode.Found, "delete always redirects back to Index");

        await _factory.WithDbAsync(async db =>
        {
            var subj = await db.Subjects.FindAsync(subjectId);
            subj.Should().NotBeNull();
            subj!.IsDeleted.Should().BeFalse(
                "service should refuse the delete because a Batch references this subject");
        });
    }
}
