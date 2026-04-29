using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.web.Models;

namespace OptimumCoaching.web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
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
            if (roles.Contains(Roles.Admin))
                return RedirectToAction(nameof(AdminDashboard));

            return RedirectToAction(nameof(UserDashboard));
        }

        [Authorize(Roles = Roles.Dev)]
        public IActionResult DevDashboard() => View();

        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Dev)]
        public IActionResult SuperAdminDashboard() => View();

        [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin + "," + Roles.Dev)]
        public IActionResult AdminDashboard() => View();

        [Authorize]
        public IActionResult UserDashboard() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
