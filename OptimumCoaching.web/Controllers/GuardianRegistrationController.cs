using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Models;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    [Authorize]
    public class GuardianRegistrationController : Controller
    {
        private readonly IGuardianService _guardianService;
        private readonly IStudentService _studentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public GuardianRegistrationController(
            IGuardianService guardianService,
            IStudentService studentService,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _guardianService = guardianService;
            _studentService = studentService;
            _userManager = userManager;
            _currentUser = currentUser;
            _env = env;
        }

        public async Task<IActionResult> Register()
        {
            var existing = await _guardianService.GetByUserIdAsync(_currentUser.UserId);
            if (existing != null) return RedirectToAction("Index", "Home");

            // If the user is already registered as a Student, send them away.
            var asStudent = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (asStudent != null) return RedirectToAction("Status", "StudentRegistration");

            var user = await _userManager.GetUserAsync(User);
            return View(new GuardianRegistrationViewModel
            {
                FullName = user?.FullName ?? string.Empty,
                Email = user?.Email
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(GuardianRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _guardianService.GetByUserIdAsync(_currentUser.UserId);
            if (existing != null) return RedirectToAction("Index", "Home");

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
                UserId = _currentUser.UserId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Relationship = model.Relationship,
                Occupation = model.Occupation,
                Address = model.Address,
                ImageUrl = imgUrl
            };

            var (success, message, _) = await _guardianService.CreateAsync(guardian, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            var u = await _userManager.GetUserAsync(User);
            if (u != null)
            {
                u.FullName = model.FullName;
                if (!string.IsNullOrEmpty(imgUrl)) u.ImageUrl = imgUrl;
                await _userManager.UpdateAsync(u);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Index", "Home");
        }
    }
}
