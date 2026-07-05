using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Student-facing dashboard for the online courses they've signed up for.
    // Separate from the offline /Student/* views so the experience matches
    // the course-shop model: a list of all enrolled online courses + a
    // per-course details page with live link, lessons, materials, etc.
    [Authorize]
    public class MyOnlineCoursesController : Controller
    {
        private readonly IStudentService _students;
        private readonly IOnlineEnrollmentService _enrollments;
        private readonly ICourseLessonService _lessons;
        private readonly IClassMaterialService _materials;
        private readonly IClassRoutineService _routine;
        private readonly ICurrentUserService _currentUser;

        public MyOnlineCoursesController(
            IStudentService students,
            IOnlineEnrollmentService enrollments,
            ICourseLessonService lessons,
            IClassMaterialService materials,
            IClassRoutineService routine,
            ICurrentUserService currentUser)
        {
            _students = students;
            _enrollments = enrollments;
            _lessons = lessons;
            _materials = materials;
            _routine = routine;
            _currentUser = currentUser;
        }

        // /MyOnlineCourses — full list of the student's online enrollments
        public async Task<IActionResult> Index()
        {
            var s = await RequireStudentAsync();
            if (s == null) return RedirectToAction("UserDashboard", "Home");

            var list = await _enrollments.GetForStudentAsync(s.Id);
            return View(list);
        }

        // /MyOnlineCourses/Details/{id} — single course workspace
        public async Task<IActionResult> Details(Guid id)
        {
            var s = await RequireStudentAsync();
            if (s == null) return RedirectToAction("UserDashboard", "Home");

            var summary = await _enrollments.GetByIdAsync(id, s.Id);
            if (summary == null) return NotFound();

            ViewBag.Lessons = await _lessons.GetForBatchAsync(summary.Batch.Id, publishedOnly: true);
            ViewBag.Materials = await _materials.GetForBatchAsync(summary.Batch.Id);
            ViewBag.RoutineSlots = await _routine.GetSlotsForBatchAsync(summary.Batch.Id);

            return View(summary);
        }

        private async Task<Student?> RequireStudentAsync()
        {
            var uid = _currentUser.UserId;
            if (uid == Guid.Empty) return null;
            var s = await _students.GetByUserIdAsync(uid);
            if (s == null) return null;
            if (s.ApprovalStatus != StudentApprovalStatus.Approved) return null;
            return s;
        }
    }
}
