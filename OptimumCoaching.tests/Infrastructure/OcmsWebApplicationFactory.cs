using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost; // ConfigureTestServices lives here
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptimumCoaching.repo;
using OptimumCoaching.web; // brings the Program class into scope for WebApplicationFactory<Program>

namespace OptimumCoaching.tests.Infrastructure;

// Spins up the full OCMS web app with three substitutions:
//   1. ApplicationDbContext → EF Core InMemory provider (no SQL Server).
//   2. Authentication → TestAuthHandler so each test sets identity via headers.
//   3. IAntiforgery     → NoOp so POSTs don't have to round-trip a token.
//
// Each factory instance gets its own InMemory database name (Guid) so
// different test classes don't share state.
public class OcmsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"ocms-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development so HTTPS redirection / production exception page
        // are skipped, matching what a developer sees locally.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // 1) Swap the DbContext to InMemory.
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // 2) Replace authentication with the Test scheme — make it the
            //    default for [Authorize] so existing controllers Just Work.
            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                opts.DefaultScheme = TestAuthHandler.SchemeName;
            });
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // 3) Bypass anti-forgery validation in tests.
            services.AddSingleton<IAntiforgery, NoOpAntiforgery>();
        });
    }

    // Convenience: scope the in-memory db so a test can seed / assert directly.
    public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await work(db);
    }

    public async Task WithDbAsync(Func<ApplicationDbContext, Task> work)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await work(db);
    }
}
