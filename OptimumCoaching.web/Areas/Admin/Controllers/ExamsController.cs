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
    public class ExamsController : AdminBaseController
    {
        private readonly IExamService _exams;
        private readonly IExamResultService _results;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ExamsController(
            IExamService exams,
            IExamResultService results,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _exams = exams; _results = results; _db = db; _currentUser = currentUser;
        }

        [Authorize(Permissions.Exams.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            var list = await _exams.GetAllAsync(batchId);
            ViewBag.ActiveBatchId = batchId;
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            return View(list);
        }

        [Authorize(Permissions.Exams.AddEdit)]
        public async Task<IActionResult> Create(Guid? batchId = null)
        {
            await PopulateOptionsAsync();
            return View(new Exam { BatchId = batchId ?? Guid.Empty, ExamDate = DateTime.UtcNow.Date });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Exams.AddEdit)]
        public async Task<IActionResult> Create(Exam model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync();
                return View(model);
            }
            var (ok, msg, _) = await _exams.CreateAsync(model, _currentUser.UserId);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, msg);
                await PopulateOptionsAsync();
                return View(model);
            }
            TempData["SuccessMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [Authorize(Permissions.Exams.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ex = await _exams.GetByIdAsync(id);
            if (ex == null) return NotFound();
            await PopulateOptionsAsync();
            return View(ex);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Exams.AddEdit)]
        public async Task<IActionResult> Edit(Exam model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync();
                return View(model);
            }
            var (ok, msg) = await _exams.UpdateAsync(model, _currentUser.UserId);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, msg);
                await PopulateOptionsAsync();
                return View(model);
            }
            TempData["SuccessMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Exams.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (ok, msg) = await _exams.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Exams.Publish)]
        public async Task<IActionResult> Publish(Guid id)
        {
            var (ok, msg) = await _exams.PublishAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // ---- Marks-entry grid + publish ----

        [Authorize(Permissions.Results.Grade)]
        public async Task<IActionResult> Grade(Guid id)
        {
            var ex = await _exams.GetByIdAsync(id);
            if (ex == null) return NotFound();

            var rows = await _results.BuildGradingGridAsync(id);
            ViewBag.Exam = ex;
            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Results.Grade)]
        public async Task<IActionResult> SaveGrades(Guid id, IList<ExamResultRow> rows)
        {
            var (ok, msg) = await _results.SaveDraftAsync(id, rows ?? new List<ExamResultRow>(), _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Grade), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Results.Publish)]
        public async Task<IActionResult> PublishResults(Guid id)
        {
            var (ok, msg) = await _results.PublishAllAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Grade), new { id });
        }

        private async Task PopulateOptionsAsync()
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.TypeOptions = Enum.GetValues<ExamType>()
                .Select(t => new SelectListItem { Value = ((int)t).ToString(), Text = t.ToString() })
                .ToList();
        }
    }
}
