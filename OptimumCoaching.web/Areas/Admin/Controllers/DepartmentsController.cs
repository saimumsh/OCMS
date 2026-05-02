using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class DepartmentsController : AdminBaseController
    {
        // Suggested presets shown on the Create form so admins can one-click pick
        // the canonical names. Editable; non-binding.
        public static readonly IReadOnlyDictionary<EducationStream, string[]> Presets =
            new Dictionary<EducationStream, string[]>
            {
                [EducationStream.Academic] = new[] { "Science", "Arts", "Commerce" },
                [EducationStream.Diploma]  = new[] { "Electrical", "Electronics", "Computer", "Civil", "Mechanical" }
            };

        private readonly IDepartmentService _service;
        private readonly ICurrentUserService _currentUser;

        public DepartmentsController(IDepartmentService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        [Authorize(Permissions.Departments.ListView)]
        public async Task<IActionResult> Index(EducationStream? stream = null)
        {
            var departments = await _service.GetAllAsync(stream);
            ViewBag.ActiveStream = stream;
            return View(departments.Select(d => new DepartmentListItem
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                Stream = d.Stream,
                IsActive = d.IsActive
            }).ToList());
        }

        [Authorize(Permissions.Departments.AddEdit)]
        public IActionResult Create(EducationStream? stream = null)
        {
            return View(new DepartmentFormViewModel
            {
                Stream = stream ?? EducationStream.Academic
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Departments.AddEdit)]
        public async Task<IActionResult> Create(DepartmentFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var department = new Department
            {
                Stream = model.Stream,
                Name = model.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim(),
                Description = model.Description
            };

            var (success, message, _) = await _service.CreateAsync(department, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { stream = model.Stream });
        }

        [Authorize(Permissions.Departments.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var d = await _service.GetByIdAsync(id);
            if (d == null) return NotFound();

            return View(new DepartmentFormViewModel
            {
                Id = d.Id,
                Stream = d.Stream,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Departments.AddEdit)]
        public async Task<IActionResult> Edit(DepartmentFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var update = new Department
            {
                Id = model.Id,
                Stream = model.Stream,
                Name = model.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(model.Code) ? null : model.Code.Trim(),
                Description = model.Description
            };

            var (success, message) = await _service.UpdateAsync(update, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { stream = model.Stream });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Departments.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _service.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
