using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class MaterialsController : AdminBaseController
    {
        private readonly IClassMaterialService _materials;
        private readonly ITopicService _topics;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public MaterialsController(
            IClassMaterialService materials,
            ITopicService topics,
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _materials = materials; _topics = topics; _db = db;
            _currentUser = currentUser; _env = env;
        }

        // /Admin/Materials?batchId={...}
        [Authorize(Permissions.Materials.ListView)]
        public async Task<IActionResult> Index(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (batchId.HasValue)
            {
                var batch = await _db.Batches.Include(b => b.Subject).FirstOrDefaultAsync(b => b.Id == batchId);
                ViewBag.Batch = batch;
                ViewBag.TopicOptions = batch?.SubjectId is { } sid
                    ? (await _topics.GetForSubjectAsync(sid))
                        .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title })
                        .ToList()
                    : new List<SelectListItem>();
            }

            var list = batchId.HasValue
                ? await _materials.GetForBatchAsync(batchId.Value)
                : new List<ClassMaterial>();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Materials.Upload)]
        public async Task<IActionResult> Create(ClassMaterial model, IFormFile? file)
        {
            if (file != null && file.Length > 0)
            {
                var (ok, msg, savedPath) = await UploadHelper.TrySaveFileAsync(_env, file, "materials", model.Id);
                if (!ok)
                {
                    TempData["ErrorMessage"] = msg;
                    return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
                }
                model.FilePath = savedPath;
            }

            var (createdOk, createdMsg, _) = await _materials.CreateAsync(model, _currentUser.UserId);
            TempData[createdOk ? "SuccessMessage" : "ErrorMessage"] = createdMsg;
            return RedirectToAction(nameof(Index), new { batchId = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Materials.Delete)]
        public async Task<IActionResult> Delete(Guid id, Guid batchId)
        {
            var (ok, msg) = await _materials.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index), new { batchId });
        }
    }
}
