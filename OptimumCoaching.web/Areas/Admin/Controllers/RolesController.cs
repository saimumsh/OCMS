using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using Perms = OptimumCoaching.core.Permissions;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class RolesController : AdminBaseController
    {
        private readonly IApplicationRoleService _roleService;
        private readonly IPermissionService _permissionService;

        public RolesController(
            IApplicationRoleService roleService,
            IPermissionService permissionService)
        {
            _roleService = roleService;
            _permissionService = permissionService;
        }

        [Authorize(Perms.UserRoles.ListView)]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleService.GetAllAsync();
            var list = new List<RoleListItem>();
            foreach (var r in roles)
            {
                var perms = await _permissionService.GetPermissionNamesByRoleAsync(r.Id);
                list.Add(new RoleListItem
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    IsSystemRole = Roles.All.Contains(r.Name),
                    PermissionCount = perms.Count
                });
            }
            return View(list);
        }

        [Authorize(Perms.UserRoles.AddEdit)]
        public IActionResult Create() => View(new CreateRoleViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Perms.UserRoles.AddEdit)]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message, _) = await _roleService.CreateRoleAsync(model.Name);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Perms.UserRoles.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var role = await _roleService.FindByIdAsync(id);
            if (role == null) return NotFound();
            return View(new EditRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                IsSystemRole = Roles.All.Contains(role.Name)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Perms.UserRoles.AddEdit)]
        public async Task<IActionResult> Edit(EditRoleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _roleService.RenameAsync(model.Id, model.Name);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Perms.UserRoles.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _roleService.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Perms.UserRoles.ManagePermissions)]
        public async Task<IActionResult> Permissions(Guid id)
        {
            var role = await _roleService.FindByIdAsync(id);
            if (role == null) return NotFound();

            var assignedNames = await _permissionService.GetPermissionNamesByRoleAsync(role.Id);

            var tree = new RolePermissionHelper();
            tree.LoadRolePermissions(assignedNames);

            return View(new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                Tree = tree,
                SelectedPermissions = assignedNames.ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Perms.UserRoles.ManagePermissions)]
        public async Task<IActionResult> Permissions(RolePermissionsViewModel model)
        {
            var role = await _roleService.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            // Resolve posted permission names → DB Permission ids.
            var allPerms = await _permissionService.GetAllAsync();
            var selectedNames = new HashSet<string>(model.SelectedPermissions ?? new(),
                StringComparer.OrdinalIgnoreCase);
            var ids = allPerms.Where(p => selectedNames.Contains(p.Name)).Select(p => p.Id);

            var (success, message) = await _permissionService.SetRolePermissionsAsync(model.RoleId, ids);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Permissions), new { id = model.RoleId });
        }
    }
}
