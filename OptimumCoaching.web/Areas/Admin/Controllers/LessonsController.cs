using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    // Manage recorded video lessons attached to a Batch. Same UX as Materials
    // (picker + inline add form) but with a publish toggle and a per-row
    // progress count for at-a-glance reach.
    public class LessonsController : AdminBaseController
    {
        private readonly ICourseLessonService _lessons;
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public LessonsController(
            ICourseLessonService lessons,
            ITopicService topics,
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _lessons = lessons; _topics = topics; _db = db;
            _currentUser = currentUser; _env = env;
        }

        // /Admin/Lessons?batchId=...
        [Authorize(Permissions.Lessons.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (!batchId.HasValue) return View(new List<CourseLesson>());

            var batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
            ViewBag.Batch = batch;
            ViewBag.TopicOptions = batch?.SubjectId is { } sid
                ? (await _topics.GetForSubjectAsync(sid))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title })
                    .ToList()
                : new List<SelectListItem>();

            return View(await _lessons.GetForBatchAsync(batchId.Value));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Lessons.Manage)]
        public async Task<IActionResult> Create(CourseLesson model, IFormFile? video, IFormFile? resource)
        {
            if (video != null && video.Length > 0)
            {
                var (ok, msg, path) = await UploadHelper.TrySaveFileAsync(_env, video, "lessons", Guid.NewGuid());
                if (!ok) { TempData["ErrorMessage"] = msg; return RedirectToAction(nameof(Index), new { batchId = model.BatchId }); }
                model.FilePath = path;
            }
            if (resource != null && resource.Length > 0)
            {
                var (ok, msg, path) = await UploadHelper.TrySaveFileAsync(_env, resource, "lesson-resources", Guid.NewGuid());
                if (!ok) { TempData["ErrorMessage"] = msg; return RedirectToAction(nameof(Index), new { batchId = model.BatchId }); }
                model.ResourcePath = path;
            }

            var (success, message, _) = await _lessons.CreateAsync(model, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Lessons.Manage)]
        public async Task<IActionResult> Publish(Guid id, bool publish, Guid batchId)
        {
            var (ok, msg) = await _lessons.PublishAsync(id, publish, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Lessons.Manage)]
        public async Task<IActionResult> Delete(Guid id, Guid batchId)
        {
            var (ok, msg) = await _lessons.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }
    }
}
