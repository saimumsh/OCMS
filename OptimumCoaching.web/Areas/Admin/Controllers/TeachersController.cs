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
    public class TeachersController : AdminBaseController
    {
        private readonly ITeacherService _teacherService;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public TeachersController(
            ITeacherService teacherService,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _teacherService = teacherService;
            _currentUser = currentUser;
            _env = env;
        }

        [Authorize(Permissions.Teachers.ListView)]
        public async Task<IActionResult> Index()
        {
            var teachers = await _teacherService.GetAllAsync();
            var list = teachers.Select(t => new TeacherListItem
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                Specialization = t.Specialization,
                ExperienceYears = t.ExperienceYears,
                IsActive = t.IsActive,
                ImageUrl = t.ImageUrl,
                HireDate = t.HireDate
            }).ToList();
            return View(list);
        }

        [Authorize(Permissions.Teachers.AddEdit)]
        public IActionResult Create() => View(new TeacherFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Teachers.AddEdit)]
        public async Task<IActionResult> Create(TeacherFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var teacherId = Guid.NewGuid();
            var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "teachers", teacherId);
            if (!imgOk)
            {
                ModelState.AddModelError(nameof(model.Image), imgMsg);
                return View(model);
            }

            var teacher = new Teacher
            {
                Id = teacherId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                Specialization = model.Specialization,
                Qualification = model.Qualification,
                ExperienceYears = model.ExperienceYears,
                HireDate = model.HireDate,
                Bio = model.Bio,
                ImageUrl = imgUrl
            };

            var (success, message, _) = await _teacherService.CreateAsync(teacher, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Teachers.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var t = await _teacherService.GetByIdAsync(id);
            if (t == null) return NotFound();

            return View(new TeacherFormViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                DateOfBirth = t.DateOfBirth,
                Gender = t.Gender,
                Address = t.Address,
                Specialization = t.Specialization,
                Qualification = t.Qualification,
                ExperienceYears = t.ExperienceYears,
                HireDate = t.HireDate,
                Bio = t.Bio,
                ImageUrl = t.ImageUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Teachers.AddEdit)]
        public async Task<IActionResult> Edit(TeacherFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _teacherService.GetByIdAsync(model.Id);
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
                var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "teachers", model.Id);
                if (!imgOk)
                {
                    ModelState.AddModelError(nameof(model.Image), imgMsg);
                    model.ImageUrl = existing.ImageUrl;
                    return View(model);
                }
                newImageUrl = imgUrl;
                oldImageToRemove = existing.ImageUrl;
            }

            var update = new Teacher
            {
                Id = model.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                Specialization = model.Specialization,
                Qualification = model.Qualification,
                ExperienceYears = model.ExperienceYears,
                HireDate = model.HireDate,
                Bio = model.Bio,
                ImageUrl = newImageUrl // null = leave unchanged, "" = clear
            };

            var (success, message) = await _teacherService.UpdateAsync(update, _currentUser.UserId);
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
        [Authorize(Permissions.Teachers.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _teacherService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Teachers.ActiveInactive)]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var t = await _teacherService.GetByIdAsync(id);
            if (t == null) return NotFound();
            var (success, message) = await _teacherService.SetActiveAsync(id, !t.IsActive, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
