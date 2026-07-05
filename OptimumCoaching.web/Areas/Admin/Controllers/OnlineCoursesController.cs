using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    // Dedicated admin area for Online (and Hybrid) batches. Reuses the
    // shared BatchFormViewModel / Batch service, but operates only on
    // batches whose DeliveryMode != Offline and forces new batches to be
    // online by default.
    public class OnlineCoursesController : AdminBaseController
    {
        private readonly IBatchService _batchService;
        private readonly IOnlineEnrollmentService _enrollments;
        private readonly IDepartmentService _departmentService;
        private readonly ITeacherService _teacherService;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public OnlineCoursesController(
            IBatchService batchService,
            IOnlineEnrollmentService enrollments,
            IDepartmentService departmentService,
            ITeacherService teacherService,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _batchService = batchService;
            _enrollments = enrollments;
            _departmentService = departmentService;
            _teacherService = teacherService;
            _db = db;
            _currentUser = currentUser;
        }

        [Authorize(Permissions.OnlineCourses.ListView)]
        public async Task<IActionResult> Index()
        {
            var batches = await _enrollments.GetOnlineBatchesAsync();
            var batchIds = batches.Select(b => b.Id).ToList();

            var enrolledByBatch = await _db.CourseEnrollments
                .Where(e => !e.IsDeleted && e.Status == EnrollmentStatus.Active && batchIds.Contains(e.BatchId))
                .GroupBy(e => e.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            var lessonsByBatch = await _db.CourseLessons
                .Where(l => !l.IsDeleted && batchIds.Contains(l.BatchId))
                .GroupBy(l => l.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            var list = batches.Select(b => new OnlineCourseListItem
            {
                Id = b.Id,
                Name = b.Name,
                DepartmentName = b.Department?.Name,
                SubjectName = b.Subject?.Name,
                TeacherName = b.Teacher?.FullName,
                DeliveryMode = b.DeliveryMode,
                CourseFee = b.CourseFee,
                OfferedPrice = b.OfferedPrice,
                OfferEndsAt = b.OfferEndsAt,
                OfferLabel = b.OfferLabel,
                CoverImageUrl = b.CoverImageUrl,
                PromoVideoUrl = b.PromoVideoUrl,
                IsPublishedForEnrollment = b.IsPublishedForEnrollment,
                IsActive = b.IsActive,
                Capacity = b.Capacity,
                EnrolledCount = enrolledByBatch.TryGetValue(b.Id, out var ec) ? ec : 0,
                LessonsCount = lessonsByBatch.TryGetValue(b.Id, out var lc) ? lc : 0
            }).ToList();

            return View(list);
        }

        [Authorize(Permissions.OnlineCourses.AddEdit)]
        public async Task<IActionResult> Create()
        {
            var model = new BatchFormViewModel
            {
                DeliveryMode = DeliveryMode.Online,
                IsPublishedForEnrollment = true
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.OnlineCourses.AddEdit)]
        public async Task<IActionResult> Create(BatchFormViewModel model)
        {
            EnsureOnlineMode(model);
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var batch = MapFormToEntity(model, Guid.NewGuid());
            var (success, message, _) = await _batchService.CreateAsync(batch, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.OnlineCourses.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var b = await _batchService.GetByIdAsync(id);
            if (b == null) return NotFound();
            if (b.DeliveryMode == DeliveryMode.Offline) return NotFound();

            var model = new BatchFormViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Description = b.Description,
                DepartmentId = b.DepartmentId,
                ClassId = b.ClassId,
                SubjectId = b.SubjectId,
                TeacherId = b.TeacherId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Capacity = b.Capacity,
                CourseFee = b.CourseFee,
                MinimumEnrollment = b.MinimumEnrollment,
                FullPaymentDiscountPercent = b.FullPaymentDiscountPercent,
                FeeDueDate = b.FeeDueDate,
                FeeDueDays = b.FeeDueDays,
                LateFeeFlat = b.LateFeeFlat,
                LateFeePerDay = b.LateFeePerDay,
                DeliveryMode = b.DeliveryMode,
                MeetingUrl = b.MeetingUrl,
                MeetingNotes = b.MeetingNotes,
                IsPublishedForEnrollment = b.IsPublishedForEnrollment,
                ShortDescription = b.ShortDescription,
                CoverImageUrl = b.CoverImageUrl,
                PromoVideoUrl = b.PromoVideoUrl,
                OfferLabel = b.OfferLabel,
                OfferedPrice = b.OfferedPrice,
                OfferEndsAt = b.OfferEndsAt
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.OnlineCourses.AddEdit)]
        public async Task<IActionResult> Edit(BatchFormViewModel model)
        {
            EnsureOnlineMode(model);
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var batch = MapFormToEntity(model, model.Id);
            var (success, message) = await _batchService.UpdateAsync(batch, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.OnlineCourses.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _batchService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.OnlineCourses.ListView)]
        public async Task<IActionResult> Details(Guid id)
        {
            var b = await _batchService.GetByIdAsync(id);
            if (b == null || b.DeliveryMode == DeliveryMode.Offline) return NotFound();

            var enrollments = await _enrollments.GetEnrollmentsForBatchAsync(id);
            var lessonsCount = await _db.CourseLessons.CountAsync(l => !l.IsDeleted && l.BatchId == id);

            var paid = await _db.StudentFeeAccounts
                .Where(a => !a.IsDeleted && a.BatchId == id)
                .SumAsync(a => (decimal?)a.AmountPaid) ?? 0m;
            var outstanding = await _db.StudentFeeAccounts
                .Where(a => !a.IsDeleted && a.BatchId == id)
                .SumAsync(a => (decimal?)(a.FinalFee - a.AmountPaid)) ?? 0m;

            return View(new OnlineCourseDetailsViewModel
            {
                Batch = b,
                Enrollments = enrollments,
                LessonsCount = lessonsCount,
                RevenuePaid = paid,
                RevenueOutstanding = outstanding
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.OnlineCourses.ManageEnrollments)]
        public async Task<IActionResult> CancelEnrollment(Guid id, Guid batchId, string? note)
        {
            var (ok, msg) = await _enrollments.CancelAsync(id, note, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Details), new { id = batchId });
        }

        // ---- Helpers ---------------------------------------------------

        private static void EnsureOnlineMode(BatchFormViewModel model)
        {
            if (model.DeliveryMode == DeliveryMode.Offline)
                model.DeliveryMode = DeliveryMode.Online;
        }

        private static Batch MapFormToEntity(BatchFormViewModel m, Guid id) => new()
        {
            Id = id,
            Name = m.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(m.Code) ? null : m.Code.Trim(),
            Description = m.Description,
            DepartmentId = m.DepartmentId,
            ClassId = m.ClassId,
            SubjectId = m.SubjectId,
            TeacherId = m.TeacherId,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            Capacity = m.Capacity,
            CourseFee = m.CourseFee,
            MinimumEnrollment = m.MinimumEnrollment,
            FullPaymentDiscountPercent = m.FullPaymentDiscountPercent,
            FeeDueDate = m.FeeDueDate,
            FeeDueDays = m.FeeDueDays,
            LateFeeFlat = m.LateFeeFlat,
            LateFeePerDay = m.LateFeePerDay,
            DeliveryMode = m.DeliveryMode,
            MeetingUrl = m.MeetingUrl,
            MeetingNotes = m.MeetingNotes,
            IsPublishedForEnrollment = m.IsPublishedForEnrollment,
            ShortDescription = m.ShortDescription,
            CoverImageUrl = m.CoverImageUrl,
            PromoVideoUrl = string.IsNullOrWhiteSpace(m.PromoVideoUrl) ? null : m.PromoVideoUrl.Trim(),
            OfferLabel = string.IsNullOrWhiteSpace(m.OfferLabel) ? null : m.OfferLabel.Trim(),
            OfferedPrice = m.OfferedPrice,
            OfferEndsAt = m.OfferEndsAt
        };

        private async Task PopulateOptionsAsync(BatchFormViewModel model)
        {
            var depts = await _departmentService.GetAllAsync();
            model.DepartmentOptions = depts
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{(d.Stream == EducationStream.Diploma ? "Diploma" : "Academic")}  {d.Name}"
                })
                .ToList();

            var teachers = await _teacherService.GetAllAsync();
            model.TeacherOptions = teachers
                .Where(t => t.IsActive)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName })
                .ToList();

            var depFilter = model.DepartmentId;
            model.ClassOptions = await _db.Classes
                .Where(c => !c.IsDeleted && (depFilter == null || c.DepartmentId == depFilter))
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            model.SubjectOptions = await _db.Subjects
                .Where(s => !s.IsDeleted && (depFilter == null || s.DepartmentId == depFilter))
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();
        }
    }
}
