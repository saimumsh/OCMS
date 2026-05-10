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
    public class TopicsController : AdminBaseController
    {
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public TopicsController(
            ITopicService topics, ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _topics = topics; _db = db; _currentUser = currentUser;
        }

        // /Admin/Topics?subjectId={...}
        [Authorize(Permissions.Topics.ListView)]
        public async Task<IActionResult> Index(Guid? subjectId = null)
        {
            ViewBag.SubjectOptions = await _db.Subjects
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();
            ViewBag.ActiveSubjectId = subjectId;

            var list = subjectId.HasValue
                ? await _topics.GetForSubjectAsync(subjectId.Value)
                : new List<Topic>();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Topics.AddEdit)]
        public async Task<IActionResult> Create(Topic model)
        {
            var (ok, msg, _) = await _topics.CreateAsync(model, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { subjectId = model.SubjectId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Topics.AddEdit)]
        public async Task<IActionResult> Update(Topic model)
        {
            var (ok, msg) = await _topics.UpdateAsync(model, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { subjectId = model.SubjectId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Topics.Delete)]
        public async Task<IActionResult> Delete(Guid id, Guid? subjectId)
        {
            var (ok, msg) = await _topics.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        // ---- Per-batch topic-teacher assignments ----

        [Authorize(Permissions.Topics.ListView)]
        public async Task<IActionResult> ForBatch(Guid batchId)
        {
            var batch = await _db.Batches
                .Include(b => b.Subject)
                .FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null) return NotFound();

            var topics = batch.SubjectId.HasValue
                ? await _topics.GetForSubjectAsync(batch.SubjectId.Value)
                : new List<Topic>();
            var assignments = await _topics.GetAssignmentsForBatchAsync(batchId);
            var teachers = await _db.Teachers
                .Where(t => !t.IsDeleted && t.IsActive)
                .OrderBy(t => t.FullName)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName })
                .ToListAsync();

            ViewBag.Batch = batch;
            ViewBag.Topics = topics;
            ViewBag.TeacherOptions = teachers;
            return View(assignments);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Topics.AssignTeachers)]
        public async Task<IActionResult> Assign(Guid batchId, Guid topicId, Guid teacherId, string? note)
        {
            var (ok, msg, _) = await _topics.AssignTeacherAsync(batchId, topicId, teacherId, note, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(ForBatch), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Topics.AssignTeachers)]
        public async Task<IActionResult> Unassign(Guid id, Guid batchId)
        {
            var (ok, msg) = await _topics.RemoveAssignmentAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(ForBatch), new { batchId });
        }
    }
}
