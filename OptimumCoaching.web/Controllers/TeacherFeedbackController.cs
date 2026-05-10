using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Student-facing endpoints to review or report a teacher. Admin-side
    // listings live under /Admin/TeacherReports — a separate controller.
    [Authorize]
    public class TeacherFeedbackController : Controller
    {
        private readonly ITeacherFeedbackService _feedback;
        private readonly IStudentService _studentService;
        private readonly ICurrentUserService _currentUser;

        public TeacherFeedbackController(
            ITeacherFeedbackService feedback,
            IStudentService studentService,
            ICurrentUserService currentUser)
        {
            _feedback = feedback; _studentService = studentService; _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Review(Guid teacherId, Guid? batchId = null)
        {
            var s = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (s == null) return Forbid();

            ViewBag.TeacherId = teacherId;
            ViewBag.BatchId = batchId;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(Guid teacherId, Guid? batchId, int rating, string? comment)
        {
            var s = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (s == null) return Forbid();

            var (ok, msg, _) = await _feedback.UpsertReviewAsync(s.Id, teacherId, batchId, rating, comment);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("UserDashboard", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Report(Guid teacherId, Guid? batchId = null)
        {
            var s = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (s == null) return Forbid();
            ViewBag.TeacherId = teacherId;
            ViewBag.BatchId = batchId;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(
            Guid teacherId, Guid? batchId, ReportCategory category, string description)
        {
            var s = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (s == null) return Forbid();

            var (ok, msg, _) = await _feedback.CreateReportAsync(s.Id, teacherId, batchId, category, description);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction("UserDashboard", "Home");
        }
    }
}
