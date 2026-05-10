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
    public class RoutineController : AdminBaseController
    {
        private readonly IClassRoutineService _routine;
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public RoutineController(
            IClassRoutineService routine,
            ITopicService topics,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _routine = routine; _topics = topics; _db = db; _currentUser = currentUser;
        }

        // /Admin/Routine?batchId={...}
        [Authorize(Permissions.Routine.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (!batchId.HasValue) return View(new RoutineViewBag());

            var batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
            ViewBag.Batch = batch;
            ViewBag.TeacherOptions = await _db.Teachers
                .Where(t => !t.IsDeleted && t.IsActive)
                .OrderBy(t => t.FullName)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName })
                .ToListAsync();
            ViewBag.TopicOptions = batch?.SubjectId is { } sid
                ? (await _topics.GetForSubjectAsync(sid))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title })
                    .ToList()
                : new List<SelectListItem>();

            var slots = await _routine.GetSlotsForBatchAsync(batchId.Value);
            var todayUtc = DateTime.UtcNow.Date;
            var overrides = await _routine.GetOverridesForBatchAsync(
                batchId.Value, from: todayUtc.AddDays(-7), to: todayUtc.AddDays(60));

            return View(new RoutineViewBag { Slots = slots, Overrides = overrides });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Routine.AddEdit)]
        public async Task<IActionResult> CreateSlot(ClassRoutineSlot model)
        {
            var (ok, msg, _) = await _routine.CreateSlotAsync(model, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Routine.AddEdit)]
        public async Task<IActionResult> UpdateSlot(ClassRoutineSlot model)
        {
            var (ok, msg) = await _routine.UpdateSlotAsync(model, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Routine.Delete)]
        public async Task<IActionResult> DeleteSlot(Guid id, Guid batchId)
        {
            var (ok, msg) = await _routine.DeleteSlotAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Routine.AddEdit)]
        public async Task<IActionResult> CreateOverride(ClassSessionOverride model)
        {
            var (ok, msg, _) = await _routine.CreateOverrideAsync(model, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Routine.Delete)]
        public async Task<IActionResult> DeleteOverride(Guid id, Guid batchId)
        {
            var (ok, msg) = await _routine.DeleteOverrideAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }
    }

    // Lightweight container the Routine view uses to render the weekly grid
    // and the upcoming overrides side-by-side.
    public class RoutineViewBag
    {
        public IList<ClassRoutineSlot> Slots { get; set; } = new List<ClassRoutineSlot>();
        public IList<ClassSessionOverride> Overrides { get; set; } = new List<ClassSessionOverride>();
    }
}
