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
            services.AddScoped<IStudentCodeService, StudentCodeService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IBatchService, BatchService>();
            services.AddScoped<IBatchUpdateService, BatchUpdateService>();
            services.AddScoped<INoticeService, NoticeService>();
            services.AddScoped<INoticeSettingsService, NoticeSettingsService>();
            services.AddScoped<INoticeTemplateService, NoticeTemplateService>();
            services.AddScoped<IMessagingService, MessagingService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IExamResultService, ExamResultService>();
            services.AddScoped<ITeacherFeedbackService, TeacherFeedbackService>();
            services.AddScoped<ITopicService, TopicService>();
            services.AddScoped<IClassMaterialService, ClassMaterialService>();
            services.AddScoped<IClassRoutineService, ClassRoutineService>();
            services.AddScoped<ICourseLessonService, CourseLessonService>();
            services.AddScoped<ILessonCommentService, LessonCommentService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<IOnlineEnrollmentService, OnlineEnrollmentService>();
            services.AddScoped<ICourseCatalogService, CourseCatalogService>();
            services.AddScoped<IBatchTeacherService, BatchTeacherService>();
            services.AddScoped<IPaymentSettingsService, PaymentSettingsService>();
            services.AddScoped<IResultDiscountTierService, ResultDiscountTierService>();
            services.AddScoped<IFeeService, FeeService>();
            services.AddScoped<IFeePaymentRequestService, FeePaymentRequestService>();
            services.AddScoped<IFeeDueAlertService, FeeDueAlertService>();
            services.AddScoped<ISalaryService, SalaryService>();
            services.AddScoped<IAttendanceService, AttendanceService>();

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

            var authBuilder = services.AddAuthentication()
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

            // Google OAuth — only register when both credentials are present so
            // the app boots cleanly in environments where Google is not configured.
            // Set Authentication:Google:ClientId / :ClientSecret in appsettings
            // (or user-secrets) after creating an OAuth 2.0 client in Google Cloud
            // Console with redirect URIs https://<host>/signin-google.
            var googleClientId = configuration["Authentication:Google:ClientId"];
            var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    // Default callback path is /signin-google — matches the URI you
                    // registered in Google Cloud Console.
                });
            }

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
            // Dev-only: populate a medium dummy dataset for analysis. No-op in
            // Production; idempotent (skipped if already seeded).
            DummyDataSeeder.SeedAsync(
                app.Services,
                app.Environment,
                app.Services.GetService<ILogger<Program>>()).GetAwaiter().GetResult();

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
