using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class PermissionsController : AdminBaseController
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [Authorize(Permissions.PermissionCatalog.ListView)]
        public async Task<IActionResult> Index()
        {
            var perms = await _permissionService.GetAllAsync();
            var rows = perms.Select(p => new PermissionListRow
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Category = p.Category,
                Description = p.Description,
                IsActive = p.IsActive
            }).ToList();
            return View(rows);
        }

        [Authorize(Permissions.PermissionCatalog.AddEdit)]
        public IActionResult Create() => View(new CreatePermissionViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.PermissionCatalog.AddEdit)]
        public async Task<IActionResult> Create(CreatePermissionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message, _) = await _permissionService.CreateAsync(
                model.Name, model.DisplayName, model.Description, model.Category);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.PermissionCatalog.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var perm = await _permissionService.FindByIdAsync(id);
            if (perm == null) return NotFound();
            return View(new EditPermissionViewModel
            {
                Id = perm.Id,
                Name = perm.Name,
                DisplayName = perm.DisplayName,
                Category = perm.Category,
                Description = perm.Description,
                IsActive = perm.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.PermissionCatalog.AddEdit)]
        public async Task<IActionResult> Edit(EditPermissionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _permissionService.UpdateAsync(
                model.Id, model.DisplayName, model.Description, model.Category, model.IsActive);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.PermissionCatalog.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _permissionService.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
