using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Models;
using OptimumCoaching.web.Models.IdentityModels;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    [Authorize]
    public class NoticesController : Controller
    {
        private readonly INoticeService _service;
        private readonly IDepartmentService _departmentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUser;

        public NoticesController(
            INoticeService service,
            IDepartmentService departmentService,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUser)
        {
            _service = service;
            _departmentService = departmentService;
            _userManager = userManager;
            _currentUser = currentUser;
        }

        [Authorize(Permissions.Notices.ListView)]
        public async Task<IActionResult> Index(NoticeAudience? audience = null)
        {
            // Teachers only see/manage notices targeted at Students.
            if (IsTeacherOnly() && audience == null) audience = NoticeAudience.Students;

            var notices = await _service.GetAllAsync(audience);
            ViewBag.ActiveAudience = audience;

            var list = notices.Select(n => new NoticeListItem
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Audience = n.Audience,
                DepartmentName = n.Department?.Name,
                IsPinned = n.IsPinned,
                IsActive = n.IsActive,
                PostedAt = n.PostedAt,
                ExpiresAt = n.ExpiresAt,
                PostedByName = n.PostedByUser?.FullName,
            }).ToList();

            return View(list);
        }

        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Create()
        {
            var model = new NoticeFormViewModel
            {
                Audience = IsTeacherOnly() ? NoticeAudience.Students : NoticeAudience.Both,
                RestrictAudienceToStudents = IsTeacherOnly()
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Create(NoticeFormViewModel model)
        {
            EnforceTeacherAudience(model);
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var notice = new Notice
            {
                Title = model.Title.Trim(),
                Body = model.Body,
                Audience = model.Audience,
                DepartmentId = model.DepartmentId,
                IsPinned = model.IsPinned,
                ExpiresAt = model.ExpiresAt
            };

            var (success, message, _) = await _service.CreateAsync(notice, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var n = await _service.GetByIdAsync(id);
            if (n == null) return NotFound();

            var model = new NoticeFormViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Audience = n.Audience,
                DepartmentId = n.DepartmentId,
                IsPinned = n.IsPinned,
                ExpiresAt = n.ExpiresAt,
                RestrictAudienceToStudents = IsTeacherOnly()
            };
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Edit(NoticeFormViewModel model)
        {
            EnforceTeacherAudience(model);
            await PopulateOptionsAsync(model);
            if (!ModelState.IsValid) return View(model);

            var notice = new Notice
            {
                Id = model.Id,
                Title = model.Title.Trim(),
                Body = model.Body,
                Audience = model.Audience,
                DepartmentId = model.DepartmentId,
                IsPinned = model.IsPinned,
                ExpiresAt = model.ExpiresAt
            };

            var (success, message) = await _service.UpdateAsync(notice, _currentUser.UserId);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _service.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // Teachers may only post notices targeted at students.
        private bool IsTeacherOnly()
        {
            var u = User.Identity;
            if (u == null) return false;
            if (u.HasRole(Roles.Admin) || u.HasRole(Roles.SuperAdmin) || u.HasRole(Roles.Dev) || u.HasRole(Roles.CC))
                return false;
            return u.HasRole(Roles.Teacher);
        }

        private void EnforceTeacherAudience(NoticeFormViewModel model)
        {
            if (IsTeacherOnly())
            {
                model.Audience = NoticeAudience.Students;
                model.RestrictAudienceToStudents = true;
            }
        }

        private async Task PopulateOptionsAsync(NoticeFormViewModel model)
        {
            var depts = await _departmentService.GetAllAsync();
            model.DepartmentOptions = depts
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
