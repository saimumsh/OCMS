using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
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
        private readonly INoticeService _noticeService;
        private readonly IExamService _examService;
        private readonly IExamResultService _examResultService;
        private readonly ITeacherFeedbackService _feedbackService;
        private readonly IClassMaterialService _materialService;
        private readonly IClassRoutineService _routineService;
        private readonly IFeeService _feeService;
        private readonly IFeeDueAlertService _dueAlertService;
        private readonly IAttendanceService _attendanceService;
        private readonly IOnlineEnrollmentService _onlineEnrollments;
        private readonly ApplicationDbContext _db;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IStudentService studentService,
            IGuardianService guardianService,
            ITeacherService teacherService,
            IBatchService batchService,
            IBatchUpdateService batchUpdateService,
            INoticeService noticeService,
            IExamService examService,
            IExamResultService examResultService,
            ITeacherFeedbackService feedbackService,
            IClassMaterialService materialService,
            IClassRoutineService routineService,
            IFeeService feeService,
            IFeeDueAlertService dueAlertService,
            IAttendanceService attendanceService,
            IOnlineEnrollmentService onlineEnrollments,
            ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _studentService = studentService;
            _guardianService = guardianService;
            _teacherService = teacherService;
            _batchService = batchService;
            _batchUpdateService = batchUpdateService;
            _noticeService = noticeService;
            _examService = examService;
            _examResultService = examResultService;
            _feedbackService = feedbackService;
            _materialService = materialService;
            _routineService = routineService;
            _feeService = feeService;
            _dueAlertService = dueAlertService;
            _attendanceService = attendanceService;
            _onlineEnrollments = onlineEnrollments;
            _db = db;
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

            // Published batches shown as an ad strip on every user dashboard.
            // Priority: active offers first, then newest publications.
            var today = DateTime.UtcNow.Date;
            vm.FeaturedCourses = await _db.Batches
                .Where(b => !b.IsDeleted && b.IsActive && b.IsPublishedForEnrollment)
                .Include(b => b.Department)
                .Include(b => b.Subject)
                .Include(b => b.Teacher)
                .OrderByDescending(b => b.OfferedPrice.HasValue
                                        && (!b.OfferEndsAt.HasValue || b.OfferEndsAt.Value >= today))
                .ThenByDescending(b => b.Created)
                .Take(6)
                .ToListAsync();

            var asStudent = await _studentService.GetByUserIdAsync(user.Id);
            if (asStudent != null)
            {
                vm.Student = asStudent;
                if (asStudent.BatchId.HasValue)
                {
                    vm.Batch = await _batchService.GetByIdAsync(asStudent.BatchId.Value);
                    vm.Updates = await _batchUpdateService.GetForBatchAsync(asStudent.BatchId.Value, take: 10);
                }
                // Auto-post overdue notices (idempotent — once per day per account).
                await _dueAlertService.EnsureAlertsForStudentAsync(asStudent.Id, user.Id);
                vm.DueAlerts = await _dueAlertService.GetForStudentAsync(asStudent.Id);

                vm.Notices = await _noticeService.GetForReceiverAsync(
                    NoticeAudience.Students, asStudent.DepartmentId, asStudent.Id, take: 10);
                vm.UpcomingExams = await _examService.GetUpcomingForStudentAsync(asStudent.Id, take: 5);
                vm.MyResults = await _examResultService.GetForStudentAsync(asStudent.Id, publishedOnly: true);

                if (asStudent.BatchId.HasValue)
                {
                    // Lazy-ensure the fee account so dashboards always show the
                    // Course fees card. Idempotent — returns the existing row
                    // when present, creates it when not (e.g., dummy data, or
                    // students enrolled before the Finance module shipped).
                    await _feeService.EnsureAccountAsync(asStudent.Id, asStudent.BatchId.Value, user.Id);

                    vm.Materials = (await _materialService.GetForBatchAsync(asStudent.BatchId.Value)).Take(8).ToList();
                    vm.RoutineSlots = await _routineService.GetSlotsForBatchAsync(asStudent.BatchId.Value);
                    vm.FeeAccount = await _feeService.GetAccountAsync(asStudent.Id, asStudent.BatchId.Value);
                    vm.ExamAdmitEligible = await _feeService.IsExamAdmitEligibleAsync(asStudent.Id, asStudent.BatchId.Value);
                    vm.AttendanceSummary = await _attendanceService.GetStudentSummaryAsync(asStudent.Id, asStudent.BatchId.Value);
                }

                // Multi-enrollment list — online/hybrid courses they've signed
                // up for via the catalog, independent of the primary batch.
                vm.MyOnlineCourses = await _onlineEnrollments.GetForStudentAsync(asStudent.Id);
            }
            else
            {
                var asTeacher = await _teacherService.GetByUserIdAsync(user.Id);
                if (asTeacher != null)
                {
                    vm.Teacher = asTeacher;
                    vm.Notices = await _noticeService.GetForReceiverAsync(
                        NoticeAudience.Teachers, departmentId: null, take: 10);
                    await PopulateTeacherDashboardAsync(vm, asTeacher.Id);
                    var rating = await _feedbackService.GetRatingSummaryAsync(asTeacher.Id);
                    vm.RatingSnapshot = new TeacherRatingSnapshot
                    {
                        Average = rating.AverageRating,
                        Count = rating.Count
                    };
                }
            }

            return View(vm);
        }

        // Loads aggregates that power the teacher dashboard tiles + batch list.
        private async Task PopulateTeacherDashboardAsync(StudentDashboardViewModel vm, Guid teacherId)
        {
            var since = DateTime.UtcNow.AddDays(-30);

            // Include batches where this teacher is either the lead OR a co-teacher.
            var coTaughtIds = _db.BatchTeachers
                .Where(bt => !bt.IsDeleted && bt.TeacherId == teacherId)
                .Select(bt => bt.BatchId);

            var batches = await _db.Batches
                .Where(b => !b.IsDeleted && (b.TeacherId == teacherId || coTaughtIds.Contains(b.Id)))
                .Include(b => b.Department)
                .Include(b => b.Subject)
                .Include(b => b.Class)
                .OrderByDescending(b => b.StartDate)
                .ToListAsync();

            var batchIds = batches.Select(b => b.Id).ToList();

            var enrollmentByBatch = await _db.Students
                .Where(s => !s.IsDeleted && s.BatchId.HasValue && batchIds.Contains(s.BatchId!.Value))
                .GroupBy(s => s.BatchId!.Value)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            var recentByBatch = await _db.BatchUpdates
                .Where(u => !u.IsDeleted && batchIds.Contains(u.BatchId) && u.PostedAt >= since)
                .GroupBy(u => u.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            vm.TeacherBatches = batches.Select(b => new TeacherBatchSummary
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                DepartmentName = b.Department?.Name,
                SubjectName = b.Subject?.Name,
                ClassName = b.Class?.Name,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Capacity = b.Capacity,
                EnrolledCount = enrollmentByBatch.TryGetValue(b.Id, out var ec) ? ec : 0,
                RecentUpdates = recentByBatch.TryGetValue(b.Id, out var ru) ? ru : 0
            }).ToList();

            vm.TotalStudentsTaught = vm.TeacherBatches.Sum(b => b.EnrolledCount);
            vm.UpdatesPostedLast30Days = vm.TeacherBatches.Sum(b => b.RecentUpdates);
            vm.ActiveBatchesCount = vm.TeacherBatches.Count(b => b.IsActiveNow);

            // Pull the teacher's most recent updates across all their batches for a feed.
            vm.Updates = await _db.BatchUpdates
                .Where(u => !u.IsDeleted && batchIds.Contains(u.BatchId))
                .Include(u => u.PostedByUser)
                .Include(u => u.Batch)
                .OrderByDescending(u => u.PostedAt)
                .Take(10)
                .ToListAsync();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
