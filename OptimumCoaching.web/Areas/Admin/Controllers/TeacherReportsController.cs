using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class TeacherReportsController : AdminBaseController
    {
        private readonly ITeacherFeedbackService _feedback;
        private readonly ICurrentUserService _currentUser;

        public TeacherReportsController(
            ITeacherFeedbackService feedback, ICurrentUserService currentUser)
        {
            _feedback = feedback; _currentUser = currentUser;
        }

        [Authorize(Permissions.TeacherReports.ListView)]
        public async Task<IActionResult> Index(ReportStatus? status = null)
        {
            var list = await _feedback.GetReportsAsync(status);
            ViewBag.ActiveStatus = status;
            return View(list);
        }

        [Authorize(Permissions.TeacherReports.ListView)]
        public async Task<IActionResult> Open(Guid id)
        {
            var r = await _feedback.GetReportByIdAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.TeacherReports.Handle)]
        public async Task<IActionResult> UpdateStatus(Guid id, ReportStatus status, string? adminNote)
        {
            var (ok, msg) = await _feedback.UpdateReportStatusAsync(id, status, adminNote, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Open), new { id });
        }
    }
}
