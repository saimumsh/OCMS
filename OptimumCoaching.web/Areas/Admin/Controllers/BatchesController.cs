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
    public class BatchesController : AdminBaseController
    {
        private readonly IBatchService _batchService;
        private readonly IBatchUpdateService _updateService;
        private readonly IDepartmentService _departmentService;
        private readonly ITeacherService _teacherService;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public BatchesController(
            IBatchService batchService,
            IBatchUpdateService updateService,
            IDepartmentService departmentService,
            ITeacherService teacherService,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _batchService = batchService;
            _updateService = updateService;
            _departmentService = departmentService;
            _teacherService = teacherService;
            _db = db;
            _currentUser = currentUser;
        }

        [Authorize(Permissions.Batches.ListView)]
        public async Task<IActionResult> Index(Guid? departmentId = null)
        {
            var batches = await _batchService.GetAllAsync(departmentId);
            var enrolledByBatch = await _db.Students
                .Where(s => s.BatchId.HasValue && !s.IsDeleted)
                .GroupBy(s => s.BatchId!.Value)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            var list = batches.Select(b => new BatchListItem
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                DepartmentId = b.DepartmentId,
                DepartmentName = b.Department?.Name,
                ClassName = b.Class?.Name,
                SubjectName = b.Subject?.Name,
                TeacherName = b.Teacher?.FullName,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Capacity = b.Capacity,
                EnrolledCount = enrolledByBatch.TryGetValue(b.Id, out var c) ? c : 0,
                IsActive = b.IsActive
            }).ToList();

            ViewBag.ActiveDepartmentId = departmentId;
            ViewBag.DepartmentOptions = await BuildDepartmentOptionsAsync();
            return View(list);
        }

        [Authorize(Permissions.Batches.AddEdit)]
        public async Task<IActionResult> Create(Guid? departmentId = null)
        {
            var model = new BatchFormViewModel { DepartmentId = departmentId };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Batches.AddEdit)]
        public async Task<IActionResult> Create(BatchFormViewModel model)
        {
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
            return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
        }

        [Authorize(Permissions.Batches.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var b = await _batchService.GetByIdAsync(id);
            if (b == null) return NotFound();

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
                Capacity = b.Capacity
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Batches.AddEdit)]
        public async Task<IActionResult> Edit(BatchFormViewModel model)
        {
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
            return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Batches.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _batchService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Batches.ListView)]
        public async Task<IActionResult> Details(Guid id)
        {
            var b = await _batchService.GetByIdAsync(id);
            if (b == null) return NotFound();

            ViewBag.Updates = await _updateService.GetForBatchAsync(id);
            ViewBag.Students = await _db.Students
                .Where(s => s.BatchId == id && !s.IsDeleted)
                .OrderBy(s => s.FullName)
                .ToListAsync();
            return View(b);
        }

        // POST a class update from the Details page.
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.BatchUpdates.Post)]
        public async Task<IActionResult> PostUpdate(PostBatchUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Title and message are required.";
                return RedirectToAction(nameof(Details), new { id = model.BatchId });
            }

            var (success, message, _) = await _updateService.PostAsync(
                model.BatchId, model.Title, model.Body,
                _currentUser.UserId == Guid.Empty ? null : _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = model.BatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.BatchUpdates.Delete)]
        public async Task<IActionResult> DeleteUpdate(Guid id, Guid batchId)
        {
            var (success, message) = await _updateService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = batchId });
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
            Capacity = m.Capacity
        };

        private async Task PopulateOptionsAsync(BatchFormViewModel model)
        {
            model.DepartmentOptions = await BuildDepartmentOptionsAsync();
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

        private async Task<IEnumerable<SelectListItem>> BuildDepartmentOptionsAsync()
        {
            var depts = await _departmentService.GetAllAsync();
            return depts
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{(d.Stream == EducationStream.Diploma ? "Diploma" : "Academic")}  {d.Name}"
                })
                .ToList();
        }
    }
}
