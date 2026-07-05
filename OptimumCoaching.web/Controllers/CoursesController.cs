using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Public-facing course catalog. Browsing is anonymous-friendly;
    // enrolling requires sign-in and a Student profile.
    [AllowAnonymous]
    public class CoursesController : Controller
    {
        private readonly ICourseCatalogService _catalog;
        private readonly IStudentService _students;
        private readonly ICurrentUserService _currentUser;

        public CoursesController(
            ICourseCatalogService catalog,
            IStudentService students,
            ICurrentUserService currentUser)
        {
            _catalog = catalog; _students = students; _currentUser = currentUser;
        }

        // /Courses — public catalog
        public async Task<IActionResult> Index()
        {
            return View(await _catalog.GetPublishedAsync());
        }

        // /Courses/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var batch = await _catalog.GetCourseDetailsAsync(id);
            if (batch == null || !batch.IsPublishedForEnrollment) return NotFound();
            return View(batch);
        }

        // POST /Courses/Enroll/{id}
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Enroll(Guid id)
        {
            var uid = _currentUser.UserId;
            if (uid == Guid.Empty) return Challenge();

            var student = await _students.GetByUserIdAsync(uid);
            if (student == null)
            {
                // Need to complete student profile first. Send them to the
                // self-registration flow with the batch hinted in TempData.
                TempData["EnrollIntent"] = id.ToString();
                TempData["ErrorMessage"] = "Please complete your student profile to enroll.";
                return RedirectToAction("Index", "StudentRegistration");
            }

            var (ok, msg) = await _catalog.EnrollAsync(student.Id, id, uid);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return ok
                ? RedirectToAction("Pay", "StudentFinance")
                : RedirectToAction(nameof(Details), new { id });
        }
    }
}
