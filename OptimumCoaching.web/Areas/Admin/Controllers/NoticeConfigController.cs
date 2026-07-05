using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    // Configuration for the Notices module — defaults + reusable templates.
    // Gated by Permissions.Notices.AddEdit (admin / teacher poster).
    public class NoticeConfigController : AdminBaseController
    {
        private readonly INoticeSettingsService _settings;
        private readonly INoticeTemplateService _templates;
        private readonly ICurrentUserService _currentUser;

        public NoticeConfigController(
            INoticeSettingsService settings,
            INoticeTemplateService templates,
            ICurrentUserService currentUser)
        {
            _settings = settings; _templates = templates; _currentUser = currentUser;
        }

        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Index() => View(await _settings.GetAsync());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Save(
            NoticeAudience defaultAudience, int defaultExpiryDays, bool defaultPinned,
            bool overdueAlertPinned, int overdueAlertExpiryDays)
        {
            var (ok, msg) = await _settings.UpdateAsync(
                defaultAudience, defaultExpiryDays, defaultPinned,
                overdueAlertPinned, overdueAlertExpiryDays, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> Templates() => View(await _templates.GetAllAsync());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> AddTemplate(string name, string title, string body, NoticeAudience defaultAudience)
        {
            var (ok, msg, _) = await _templates.CreateAsync(name, title, body, defaultAudience, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Templates));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> UpdateTemplate(Guid id, string name, string title, string body, NoticeAudience defaultAudience)
        {
            var (ok, msg) = await _templates.UpdateAsync(id, name, title, body, defaultAudience, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Templates));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Notices.AddEdit)]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            var (ok, msg) = await _templates.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Templates));
        }
    }
}
