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
    public class AssignmentsController : AdminBaseController
    {
        private readonly IAssignmentService _assignments;
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public AssignmentsController(
            IAssignmentService assignments,
            ITopicService topics,
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _assignments = assignments; _topics = topics; _db = db;
            _currentUser = currentUser; _env = env;
        }

        [Authorize(Permissions.Assignments.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (!batchId.HasValue) return View(new List<Assignment>());

            var batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
            ViewBag.Batch = batch;
            ViewBag.TopicOptions = batch?.SubjectId is { } sid
                ? (await _topics.GetForSubjectAsync(sid))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title })
                    .ToList()
                : new List<SelectListItem>();

            return View(await _assignments.GetForBatchAsync(batchId.Value));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Assignments.Manage)]
        public async Task<IActionResult> Create(Assignment model, IFormFile? attachment)
        {
            if (attachment != null && attachment.Length > 0)
            {
                var (ok, msg, path) = await UploadHelper.TrySaveFileAsync(_env, attachment, "assignments", Guid.NewGuid());
                if (!ok) { TempData["ErrorMessage"] = msg; return RedirectToAction(nameof(Index), new { batchId = model.BatchId }); }
                model.AttachmentPath = path;
            }

            var (success, message, _) = await _assignments.CreateAsync(model, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Assignments.Manage)]
        public async Task<IActionResult> Publish(Guid id, bool publish, Guid batchId)
        {
            var (ok, msg) = await _assignments.PublishAsync(id, publish, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Assignments.Manage)]
        public async Task<IActionResult> Delete(Guid id, Guid batchId)
        {
            var (ok, msg) = await _assignments.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }

        // /Admin/Assignments/Submissions/{id}
        [Authorize(Permissions.Assignments.Grade)]
        public async Task<IActionResult> Submissions(Guid id)
        {
            var a = await _assignments.GetByIdAsync(id);
            if (a == null) return NotFound();
            ViewBag.Assignment = a;
            return View(await _assignments.GetSubmissionsAsync(id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Assignments.Grade)]
        public async Task<IActionResult> Grade(Guid id, decimal? score, string? feedback, Guid assignmentId)
        {
            var (ok, msg) = await _assignments.GradeAsync(id, score, feedback, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Submissions), new { id = assignmentId });
        }
    }
}
