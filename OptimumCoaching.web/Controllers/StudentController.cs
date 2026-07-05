using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Read-only student-facing screens — routine, materials, attendance,
    // exams, results, notices — all scoped to the signed-in student's
    // current batch. Sidebar links live in _SidebarPartial under
    // "My Course". Non-students are redirected to the dashboard.
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _students;
        private readonly IClassRoutineService _routine;
        private readonly IClassMaterialService _materials;
        private readonly IAttendanceService _attendance;
        private readonly IExamService _exams;
        private readonly IExamResultService _results;
        private readonly INoticeService _notices;
        private readonly ICurrentUserService _currentUser;
        private readonly ApplicationDbContext _db;

        public StudentController(
            IStudentService students,
            IClassRoutineService routine,
            IClassMaterialService materials,
            IAttendanceService attendance,
            IExamService exams,
            IExamResultService results,
            INoticeService notices,
            ICurrentUserService currentUser,
            ApplicationDbContext db)
        {
            _students = students;
            _routine = routine;
            _materials = materials;
            _attendance = attendance;
            _exams = exams;
            _results = results;
            _notices = notices;
            _currentUser = currentUser;
            _db = db;
        }

        public IActionResult Index() => RedirectToAction("UserDashboard", "Home");

        // /Student/Routine — weekly class schedule + upcoming overrides
        public async Task<IActionResult> Routine()
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var slots = await _routine.GetSlotsForBatchAsync(s.BatchId!.Value);
            var today = DateTime.UtcNow.Date;
            var overrides = await _routine.GetOverridesForBatchAsync(
                s.BatchId.Value, from: today.AddDays(-7), to: today.AddDays(60));

            ViewBag.Student = s;
            ViewBag.Batch = s.Batch;
            ViewBag.Overrides = overrides;
            return View(slots);
        }

        // /Student/Materials — every uploaded resource for the batch
        public async Task<IActionResult> Materials()
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var list = await _materials.GetForBatchAsync(s.BatchId!.Value);
            ViewBag.Batch = s.Batch;
            return View(list);
        }

        // /Student/Attendance — summary + per-session history (only sessions
        // where the student has an actual record)
        public async Task<IActionResult> Attendance()
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var summary = await _attendance.GetStudentSummaryAsync(s.Id, s.BatchId);
            var records = await _db.AttendanceRecords
                .Include(r => r.Session)
                    .ThenInclude(sess => sess.Topic)
                .Where(r => !r.IsDeleted && r.StudentId == s.Id)
                .OrderByDescending(r => r.Session.SessionDate)
                .Take(60)
                .ToListAsync();

            ViewBag.Summary = summary;
            ViewBag.Batch = s.Batch;
            return View(records);
        }

        // /Student/Exams — upcoming + past exams for the batch
        public async Task<IActionResult> Exams()
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var today = DateTime.UtcNow.Date;
            var all = await _db.Exams
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Where(e => !e.IsDeleted && e.BatchId == s.BatchId
                            && e.Status != ExamStatus.Cancelled)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            ViewBag.Upcoming = all.Where(e => e.ExamDate >= today).OrderBy(e => e.ExamDate).ToList();
            ViewBag.Past     = all.Where(e => e.ExamDate < today).ToList();
            ViewBag.Batch = s.Batch;
            return View();
        }

        // /Student/Results — published results only
        public async Task<IActionResult> Results()
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var rows = await _results.GetForStudentAsync(s.Id, publishedOnly: true);
            ViewBag.Student = s;
            return View(rows);
        }

        // /Student/Lessons — list published lessons with per-student progress
        public async Task<IActionResult> Lessons([FromServices] ICourseLessonService lessons)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var list = await lessons.GetForStudentAsync(s.Id, s.BatchId!.Value);
            ViewBag.Batch = s.Batch;
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLessonComplete(
            Guid id, bool completed, [FromServices] ICourseLessonService lessons)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return Forbid();
            await lessons.SetCompletedAsync(id, s.Id, completed);
            return RedirectToAction(nameof(Lessons));
        }

        // /Student/Lesson/{id} — single lesson detail + comments thread
        public async Task<IActionResult> Lesson(
            Guid id,
            [FromServices] ICourseLessonService lessons,
            [FromServices] ILessonCommentService comments)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var lesson = await lessons.GetByIdAsync(id);
            if (lesson == null || lesson.BatchId != s.BatchId) return NotFound();

            await lessons.MarkOpenedAsync(id, s.Id);
            ViewBag.Comments = await comments.GetForLessonAsync(id);
            return View(lesson);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment(
            Guid lessonId, string body, Guid? parentCommentId,
            [FromServices] ILessonCommentService comments)
        {
            if (_currentUser.UserId == Guid.Empty) return Challenge();
            var (ok, msg, _) = await comments.AddAsync(lessonId, _currentUser.UserId, body, parentCommentId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Lesson), new { id = lessonId });
        }

        // /Student/Assignments — list published assignments + my submission status
        public async Task<IActionResult> Assignments([FromServices] IAssignmentService svc)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var list = await svc.GetForStudentAsync(s.Id);
            var subs = new Dictionary<Guid, AssignmentSubmission?>();
            foreach (var a in list)
                subs[a.Id] = await svc.GetSubmissionAsync(a.Id, s.Id);

            ViewBag.Submissions = subs;
            ViewBag.Batch = s.Batch;
            return View(list);
        }

        // /Student/Assignment/{id} — detail + submit form
        public async Task<IActionResult> Assignment(Guid id, [FromServices] IAssignmentService svc)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return RedirectToAction("UserDashboard", "Home");

            var a = await svc.GetByIdAsync(id);
            if (a == null || a.BatchId != s.BatchId) return NotFound();

            ViewBag.Submission = await svc.GetSubmissionAsync(id, s.Id);
            return View(a);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(
            Guid id, string? responseText, Microsoft.AspNetCore.Http.IFormFile? file,
            [FromServices] IAssignmentService svc,
            [FromServices] Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            var s = await RequireStudentBatchAsync();
            if (s is null) return Forbid();

            string? filePath = null;
            if (file != null && file.Length > 0)
            {
                var (ok, msg, path) = await OptimumCoaching.web.Core.UploadHelper.TrySaveFileAsync(
                    env, file, "assignment-submissions", Guid.NewGuid());
                if (!ok) { TempData["ErrorMessage"] = msg; return RedirectToAction(nameof(Assignment), new { id }); }
                filePath = path;
            }

            var (success, message, _) = await svc.SubmitAsync(id, s.Id, responseText, filePath);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Assignment), new { id });
        }

        // /Student/Notices — every notice the student is eligible to see
        public async Task<IActionResult> Notices()
        {
            var user = _currentUser.UserId;
            if (user == Guid.Empty) return RedirectToAction("UserDashboard", "Home");
            var s = await _students.GetByUserIdAsync(user);
            if (s == null) return RedirectToAction("UserDashboard", "Home");

            var list = await _notices.GetForReceiverAsync(
                NoticeAudience.Students, s.DepartmentId, s.Id, take: 100);
            return View(list);
        }

        // Returns the student row WITH Batch + Department included, or null if
        // the user isn't an approved student attached to a batch.
        private async Task<Student?> RequireStudentBatchAsync()
        {
            var uid = _currentUser.UserId;
            if (uid == Guid.Empty) return null;
            var s = await _students.GetByUserIdAsync(uid);
            if (s == null || !s.BatchId.HasValue) return null;
            if (s.ApprovalStatus != StudentApprovalStatus.Approved) return null;
            return s;
        }
    }
}
