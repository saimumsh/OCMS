using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Models;

namespace OptimumCoaching.web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStudentService _studentService;
        private readonly IGuardianService _guardianService;
        private readonly ITeacherService _teacherService;
        private readonly IBatchService _batchService;
        private readonly IBatchUpdateService _batchUpdateService;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IStudentService studentService,
            IGuardianService guardianService,
            ITeacherService teacherService,
            IBatchService batchService,
            IBatchUpdateService batchUpdateService)
        {
            _logger = logger;
            _userManager = userManager;
            _studentService = studentService;
            _guardianService = guardianService;
            _teacherService = teacherService;
            _batchService = batchService;
            _batchUpdateService = batchUpdateService;
        }

        [AllowAnonymous]
        public IActionResult Index() => View();

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);

            // Highest-privilege role wins.
            if (roles.Contains(Roles.Dev))
                return RedirectToAction(nameof(DevDashboard));
            if (roles.Contains(Roles.SuperAdmin))
                return RedirectToAction(nameof(SuperAdminDashboard));
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.CC))
                return RedirectToAction(nameof(AdminDashboard));

            // Plain User role: route based on their domain profile.
            var asTeacher = await _teacherService.GetByUserIdAsync(user.Id);
            if (asTeacher != null) return RedirectToAction(nameof(UserDashboard));

            var asStudent = await _studentService.GetByUserIdAsync(user.Id);
            if (asStudent != null)
            {
                return asStudent.ApprovalStatus == StudentApprovalStatus.Approved
                    ? RedirectToAction(nameof(UserDashboard))
                    : RedirectToAction("Status", "StudentRegistration");
            }

            var asGuardian = await _guardianService.GetByUserIdAsync(user.Id);
            if (asGuardian != null) return RedirectToAction(nameof(UserDashboard));

            // No domain profile yet — send them to the Complete-your-profile chooser.
            return RedirectToAction("Index", "StudentRegistration");
        }

        [Authorize(Roles = Roles.Dev)]
        public IActionResult DevDashboard() => View();

        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Dev)]
        public IActionResult SuperAdminDashboard() => View();

        [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin + "," + Roles.Dev + "," + Roles.CC)]
        public IActionResult AdminDashboard() => View();

        [Authorize]
        public async Task<IActionResult> UserDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var vm = new StudentDashboardViewModel { FullName = user.FullName };

            var asStudent = await _studentService.GetByUserIdAsync(user.Id);
            if (asStudent != null)
            {
                vm.Student = asStudent;
                if (asStudent.BatchId.HasValue)
                {
                    vm.Batch = await _batchService.GetByIdAsync(asStudent.BatchId.Value);
                    vm.Updates = await _batchUpdateService.GetForBatchAsync(asStudent.BatchId.Value, take: 10);
                }
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
