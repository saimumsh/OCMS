using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;
using OptimumCoaching.service;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Models.IdentityModels;
using OptimumCoaching.web.Services;
using System.Text;

namespace OptimumCoaching.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            var services = builder.Services;

            // Strongly-typed settings
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            // HTTP / DI
            services.AddHttpContextAccessor();
            services.AddTransient<ICurrentUserService, CurrentUserService>();

            // Generic repository + UnitOfWork (open generic; resolves IRepository<T> for any T : Entity)
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Identity-related app services
            services.AddTransient<IApplicationUserService, ApplicationUserService>();
            services.AddTransient<IApplicationRoleService, ApplicationRoleService>();
            services.AddScoped<IPermissionService, PermissionService>();

            // Domain services
            services.AddScoped<ITeacherService, TeacherService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IBatchService, BatchService>();
            services.AddScoped<IBatchUpdateService, BatchUpdateService>();

            // Permission-based authorization (VitalityCash-style policy provider)
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // EF Core + Identity
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("OptimumCoachingConnStr"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 5;
                    options.Password.RequiredUniqueChars = 0;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>();

            // Dual auth: Cookie + JWT (cookie is added by AddDefaultIdentity; JWT layered on)
            var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();
            var key = Encoding.ASCII.GetBytes(
                string.IsNullOrWhiteSpace(appSettings.TokenSecretKey)
                    ? "default-token-key-please-change"
                    : appSettings.TokenSecretKey);

            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            services.AddControllersWithViews();
            services.AddRazorPages();

            var app = builder.Build();

            IdentitySeeder.SeedAsync(app.Services).GetAwaiter().GetResult();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                // Force HTTPS only in production. In dev, both http and https
                // are served so the app is reachable without a trusted dev cert.
                app.UseHttpsRedirection();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areaRoute",
                pattern: "{area:exists}/{controller}/{action}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            app.Run();
        }

        public static string AppVersion() => "1.0.0";
        public static string ProjectName() => "OCC";
        public static string ApplicantName() => "Optimum Coaching";
    }
}
