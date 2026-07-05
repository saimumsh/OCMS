using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class AttendanceController : AdminBaseController
    {
        private readonly IAttendanceService _attendance;
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public AttendanceController(
            IAttendanceService attendance,
            ITopicService topics,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _attendance = attendance; _topics = topics; _db = db; _currentUser = currentUser;
        }

        // /Admin/Attendance?batchId=...
        [Authorize(Permissions.Attendance.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (!batchId.HasValue) return View(new List<AttendanceSession>());

            ViewBag.Batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
            var sessions = await _attendance.GetSessionsForBatchAsync(
                batchId.Value, from: DateTime.UtcNow.AddMonths(-3));
            return View(sessions);
        }

        // /Admin/Attendance/Mark?batchId=...&date=2026-05-12
        [Authorize(Permissions.Attendance.Mark)]
        public async Task<IActionResult> Mark(Guid batchId, DateTime? date = null)
        {
            if (batchId == Guid.Empty) return RedirectToAction(nameof(Index));

            var when = (date ?? DateTime.UtcNow).Date;
            var grid = await _attendance.BuildMarkingGridAsync(batchId, when);

            var batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
            ViewBag.Batch = batch;
            ViewBag.TopicOptions = batch?.SubjectId is { } sid
                ? (await _topics.GetForSubjectAsync(sid))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title })
                    .ToList()
                : new List<SelectListItem>();

            return View(grid);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Attendance.Mark)]
        public async Task<IActionResult> Save(Guid batchId, DateTime date, Guid? topicId, string? note,
            IList<AttendanceRowInput> rows)
        {
            var (ok, msg, _) = await _attendance.SaveAsync(
                batchId, date, topicId, note, rows ?? new List<AttendanceRowInput>(), _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Mark), new { batchId, date = date.ToString("yyyy-MM-dd") });
        }

        // /Admin/Attendance/Session/{id}
        [Authorize(Permissions.Attendance.ListView)]
        public async Task<IActionResult> Session(Guid id)
        {
            var session = await _attendance.GetSessionAsync(id);
            if (session == null) return NotFound();
            return View(session);
        }
    }
}
