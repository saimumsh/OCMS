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
    public class SubjectsController : AdminBaseController
    {
        private readonly ISubjectService _service;
        private readonly IDepartmentService _departmentService;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public SubjectsController(
            ISubjectService service,
            IDepartmentService departmentService,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _service = service;
            _departmentService = departmentService;
            _db = db;
            _currentUser = currentUser;
        }

        [Authorize(Permissions.Subjects.ListView)]
        public async Task<IActionResult> Index(Guid? departmentId = null)
        {
            var subjects = await _service.GetAllAsync(departmentId);
            ViewBag.ActiveDepartmentId = departmentId;
            ViewBag.DepartmentOptions = await BuildDepartmentOptionsAsync();

            var list = subjects.Select(s => new SubjectListItem
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department?.Name,
                DepartmentStream = s.Department?.Stream,
                ClassId = s.ClassId,
                ClassName = s.Class?.Name,
                IsActive = s.IsActive
            }).ToList();

            return View(list);
        }

        [Authorize(Permissions.Subjects.AddEdit)]
        public async Task<IActionResult> Create(Guid? departmentId = null)
        {
            var model = new SubjectFormViewModel { DepartmentId = departmentId };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Subjects.AddEdit)]
        public async Task<IActionResult> Create(SubjectFormViewModel model)
        {
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var subject = new Subject
            {
                Name = model.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim(),
                Description = model.Description,
                DepartmentId = model.DepartmentId,
                ClassId = model.ClassId
            };

            var (success, message, _) = await _service.CreateAsync(subject, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
        }

        [Authorize(Permissions.Subjects.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var s = await _service.GetByIdAsync(id);
            if (s == null) return NotFound();

            var model = new SubjectFormViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
                DepartmentId = s.DepartmentId,
                ClassId = s.ClassId
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Subjects.AddEdit)]
        public async Task<IActionResult> Edit(SubjectFormViewModel model)
        {
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var subject = new Subject
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim(),
                Description = model.Description,
                DepartmentId = model.DepartmentId,
                ClassId = model.ClassId
            };

            var (success, message) = await _service.UpdateAsync(subject, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Subjects.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _service.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // JSON endpoint used by the form to populate the Class dropdown when the
        // user changes the Department selection.
        [HttpGet, Authorize(Permissions.Subjects.AddEdit)]
        public async Task<IActionResult> ClassesForDepartment(Guid? departmentId)
        {
            var classes = await _db.Classes
                .Where(c => !c.IsDeleted && (departmentId == null || c.DepartmentId == departmentId))
                .OrderBy(c => c.Name)
                .Select(c => new { value = c.Id, text = c.Name })
                .ToListAsync();
            return Json(classes);
        }

        private async Task PopulateOptionsAsync(SubjectFormViewModel model)
        {
            model.DepartmentOptions = await BuildDepartmentOptionsAsync();

            var depFilter = model.DepartmentId;
            model.ClassOptions = await _db.Classes
                .Where(c => !c.IsDeleted && (depFilter == null || c.DepartmentId == depFilter))
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
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
