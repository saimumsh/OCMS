using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    // Configuration screens for the payments module — receipt numbering,
    // enabled methods, and the result-discount tier table. Gated by
    // Permissions.Finance.ManageFees (admins only by default).
    public class PaymentConfigController : AdminBaseController
    {
        private readonly IPaymentSettingsService _settings;
        private readonly IResultDiscountTierService _tiers;
        private readonly ICurrentUserService _currentUser;

        public PaymentConfigController(
            IPaymentSettingsService settings,
            IResultDiscountTierService tiers,
            ICurrentUserService currentUser)
        {
            _settings = settings; _tiers = tiers; _currentUser = currentUser;
        }

        // /Admin/PaymentConfig — settings page
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> Index()
        {
            ViewBag.AllMethods = Enum.GetValues<PaymentMethod>();
            ViewBag.EnabledMethods = await _settings.GetEnabledMethodsAsync();
            return View(await _settings.GetAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> Save(
            string currencySymbol, string receiptPrefix, int nextReceiptNumber,
            int[]? enabledMethods)
        {
            var csv = enabledMethods is { Length: > 0 }
                ? string.Join(",", enabledMethods)
                : null; // null = all enabled
            var (ok, msg) = await _settings.UpdateAsync(
                currencySymbol, receiptPrefix, nextReceiptNumber, csv, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // /Admin/PaymentConfig/Tiers — result-discount tier table
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> Tiers()
        {
            return View(await _tiers.GetAllAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> AddTier(string name, decimal minResultPercent, decimal discountPercent)
        {
            var (ok, msg, _) = await _tiers.CreateAsync(name, minResultPercent, discountPercent, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Tiers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> UpdateTier(Guid id, string name, decimal minResultPercent, decimal discountPercent)
        {
            var (ok, msg) = await _tiers.UpdateAsync(id, name, minResultPercent, discountPercent, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Tiers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> DeleteTier(Guid id)
        {
            var (ok, msg) = await _tiers.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Tiers));
        }
    }
}
