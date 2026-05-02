using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using OptimumCoaching.web.Core;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly IApplicationUserService _userService;
        private readonly IApplicationRoleService _roleService;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            IApplicationUserService userService,
            IApplicationRoleService roleService,
            IWebHostEnvironment env)
        {
            _userService = userService;
            _roleService = roleService;
            _env = env;
        }

        [Authorize(Permissions.Users.ListView)]
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            var list = new List<UserListItem>();
            foreach (var u in users)
            {
                list.Add(new UserListItem
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    ImageUrl = u.ImageUrl,
                    IsActive = u.IsActive,
                    Status = u.Status,
                    Roles = await _userService.GetRolesAsync(u)
                });
            }
            return View(list);
        }

        [Authorize(Permissions.Users.AddEdit)]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleService.GetAllAsync();
            return View(new CreateUserViewModel
            {
                AllRoles = roles.Select(r => r.Name ?? string.Empty).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Users.AddEdit)]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            var imageId = Guid.NewGuid();
            var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "users", imageId);
            if (!imgOk)
            {
                ModelState.AddModelError(nameof(model.Image), imgMsg);
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            var (success, message, _) = await _userService.CreateAsync(
                model.FullName, model.Email, model.Password, model.SelectedRoles ?? new(), imgUrl);

            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Users.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userService.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = await _roleService.GetAllAsync();
            var userRoles = await _userService.GetRolesAsync(user);

            return View(new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                ImageUrl = user.ImageUrl,
                IsActive = user.IsActive,
                Status = user.Status,
                SelectedRoles = userRoles.ToList(),
                AllRoles = allRoles.Select(r => r.Name ?? string.Empty).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Users.AddEdit)]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            var existing = await _userService.FindByIdAsync(model.Id);
            if (existing == null) return NotFound();

            string? newImageUrl = null;       // null  = leave unchanged
            string? oldImageToRemove = null;

            if (model.RemoveImage)
            {
                newImageUrl = string.Empty;   // empty = clear
                oldImageToRemove = existing.ImageUrl;
            }

            if (model.Image != null)
            {
                var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "users", model.Id);
                if (!imgOk)
                {
                    ModelState.AddModelError(nameof(model.Image), imgMsg);
                    model.ImageUrl = existing.ImageUrl;
                    model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                    return View(model);
                }
                newImageUrl = imgUrl;
                oldImageToRemove = existing.ImageUrl;
            }

            var update = await _userService.UpdateAsync(model.Id, model.FullName, model.IsActive, model.Status, newImageUrl);
            if (!update.Success)
            {
                if (!string.IsNullOrEmpty(newImageUrl)) UploadHelper.TryDeleteImage(_env, newImageUrl);
                ModelState.AddModelError(string.Empty, update.Message);
                model.ImageUrl = existing.ImageUrl;
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            if (!string.IsNullOrEmpty(oldImageToRemove) && oldImageToRemove != newImageUrl)
                UploadHelper.TryDeleteImage(_env, oldImageToRemove);

            var setRoles = await _userService.SetRolesAsync(model.Id, model.SelectedRoles ?? new());
            if (!setRoles.Success)
            {
                ModelState.AddModelError(string.Empty, setRoles.Message);
                model.ImageUrl = newImageUrl ?? existing.ImageUrl;
                model.AllRoles = (await _roleService.GetAllAsync()).Select(r => r.Name ?? string.Empty).ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = "User updated";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Users.ResetPassword)]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var user = await _userService.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(new ResetUserPasswordViewModel { Id = user.Id, Email = user.Email ?? string.Empty });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Users.ResetPassword)]
        public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _userService.ResetPasswordAsync(model.Id, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Users.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _userService.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

    }
}
