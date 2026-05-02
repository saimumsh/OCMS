using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class GuardiansController : AdminBaseController
    {
        private readonly IGuardianService _guardianService;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public GuardiansController(
            IGuardianService guardianService,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _guardianService = guardianService;
            _currentUser = currentUser;
            _env = env;
        }

        [Authorize(Permissions.Guardians.ListView)]
        public async Task<IActionResult> Index()
        {
            var guardians = await _guardianService.GetAllAsync();
            var list = guardians.Select(g => new GuardianListItem
            {
                Id = g.Id,
                FullName = g.FullName,
                Email = g.Email,
                PhoneNumber = g.PhoneNumber,
                Relationship = g.Relationship,
                ImageUrl = g.ImageUrl,
                IsActive = g.IsActive,
                Created = g.Created
            }).ToList();
            return View(list);
        }

        [Authorize(Permissions.Guardians.AddEdit)]
        public IActionResult Create() => View(new GuardianFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Guardians.AddEdit)]
        public async Task<IActionResult> Create(GuardianFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var guardianId = Guid.NewGuid();
            var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "guardians", guardianId);
            if (!imgOk)
            {
                ModelState.AddModelError(nameof(model.Image), imgMsg);
                return View(model);
            }

            var guardian = new Guardian
            {
                Id = guardianId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Relationship = model.Relationship,
                Occupation = model.Occupation,
                Address = model.Address,
                Notes = model.Notes,
                ImageUrl = imgUrl
            };

            var (success, message, _) = await _guardianService.CreateAsync(guardian, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Guardians.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var g = await _guardianService.GetByIdAsync(id);
            if (g == null) return NotFound();

            return View(new GuardianFormViewModel
            {
                Id = g.Id,
                FullName = g.FullName,
                Email = g.Email,
                PhoneNumber = g.PhoneNumber,
                Relationship = g.Relationship,
                Occupation = g.Occupation,
                Address = g.Address,
                Notes = g.Notes,
                ImageUrl = g.ImageUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Guardians.AddEdit)]
        public async Task<IActionResult> Edit(GuardianFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _guardianService.GetByIdAsync(model.Id);
            if (existing == null) return NotFound();

            string? newImageUrl = null;
            string? oldImageToRemove = null;

            if (model.RemoveImage)
            {
                newImageUrl = string.Empty;
                oldImageToRemove = existing.ImageUrl;
            }

            if (model.Image != null)
            {
                var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "guardians", model.Id);
                if (!imgOk)
                {
                    ModelState.AddModelError(nameof(model.Image), imgMsg);
                    model.ImageUrl = existing.ImageUrl;
                    return View(model);
                }
                newImageUrl = imgUrl;
                oldImageToRemove = existing.ImageUrl;
            }

            var update = new Guardian
            {
                Id = model.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Relationship = model.Relationship,
                Occupation = model.Occupation,
                Address = model.Address,
                Notes = model.Notes,
                ImageUrl = newImageUrl
            };

            var (success, message) = await _guardianService.UpdateAsync(update, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(newImageUrl)) UploadHelper.TryDeleteImage(_env, newImageUrl);
                ModelState.AddModelError(string.Empty, message);
                model.ImageUrl = existing.ImageUrl;
                return View(model);
            }

            if (!string.IsNullOrEmpty(oldImageToRemove) && oldImageToRemove != newImageUrl)
                UploadHelper.TryDeleteImage(_env, oldImageToRemove);

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Guardians.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _guardianService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
