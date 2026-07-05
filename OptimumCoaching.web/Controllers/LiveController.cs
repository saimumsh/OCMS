using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Handles the student/teacher "Join live class" flow. Logs an attendance
    // record then redirects to the resolved meeting URL.
    //
    // Resolution order for the meeting URL:
    //   1. A ClassSessionOverride for today on the same batch (with a URL).
    //   2. The current ClassRoutineSlot (within its time window).
    //   3. The Batch's default MeetingUrl.
    [Authorize]
    public class LiveController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IStudentService _students;
        private readonly IAttendanceService _attendance;
        private readonly ICurrentUserService _currentUser;

        public LiveController(
            ApplicationDbContext db,
            IStudentService students,
            IAttendanceService attendance,
            ICurrentUserService currentUser)
        {
            _db = db; _students = students;
            _attendance = attendance; _currentUser = currentUser;
        }

        // /Live/Join/{batchId}
        public async Task<IActionResult> Join(Guid id)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (batch == null) return NotFound();

            // Pick the URL with the strictest scope that's present.
            var today = DateTime.UtcNow.Date;
            var now = DateTime.UtcNow.TimeOfDay;

            var todayOverride = await _db.ClassSessionOverrides
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.BatchId == id
                                          && o.SessionDate == today
                                          && !string.IsNullOrEmpty(o.MeetingUrl));

            var currentSlot = await _db.ClassRoutineSlots
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.BatchId == id
                                          && (int)s.Day == (int)DateTime.UtcNow.DayOfWeek
                                          && s.StartTime <= now && s.EndTime >= now);

            var url = todayOverride?.MeetingUrl
                   ?? currentSlot?.MeetingUrl
                   ?? batch.MeetingUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                TempData["ErrorMessage"] = "No meeting link configured for this batch right now.";
                return RedirectToAction("UserDashboard", "Home");
            }

            // If the joiner is a student in this batch, stamp attendance for
            // today as Present so live attendance is captured implicitly.
            var uid = _currentUser.UserId;
            if (uid != Guid.Empty)
            {
                var student = await _students.GetByUserIdAsync(uid);
                if (student != null && student.BatchId == id)
                {
                    await _attendance.SaveAsync(
                        id, today, currentSlot?.TopicId, "Auto-marked via Join Live",
                        new List<AttendanceRowInput>
                        {
                            new() { StudentId = student.Id, Status = AttendanceStatus.Present }
                        },
                        uid);
                }
            }

            return Redirect(url);
        }
    }
}
