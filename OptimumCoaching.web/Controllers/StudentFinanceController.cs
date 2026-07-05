using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    // Student-facing payment-submission flow. Students view their own fee
    // accounts, submit a payment with a receipt image, and see their request
    // history.
    [Authorize]
    public class StudentFinanceController : Controller
    {
        private readonly IFeeService _fees;
        private readonly IFeePaymentRequestService _requests;
        private readonly IPaymentSettingsService _paySettings;
        private readonly IStudentService _students;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public StudentFinanceController(
            IFeeService fees,
            IFeePaymentRequestService requests,
            IPaymentSettingsService paySettings,
            IStudentService students,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _fees = fees; _requests = requests; _paySettings = paySettings;
            _students = students; _currentUser = currentUser; _env = env;
        }

        // /Finance/Pay — pick a fee account to submit a payment for
        public async Task<IActionResult> Pay(Guid? accountId = null)
        {
            var student = await _students.GetByUserIdAsync(_currentUser.UserId);
            if (student == null) return RedirectToAction("UserDashboard", "Home");

            var accounts = await _fees.GetForStudentAsync(student.Id);
            ViewBag.Accounts = accounts;
            ViewBag.EnabledMethods = await _paySettings.GetEnabledMethodsAsync();
            ViewBag.SelectedAccountId = accountId;
            return View();
        }

        // POST /Finance/Pay — submit the request
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
            Guid accountId, decimal amount, PaymentMethod method,
            string? transactionReference, string? note, IFormFile? receipt)
        {
            string? receiptPath = null;
            if (receipt != null && receipt.Length > 0)
            {
                var (okFile, fileMsg, savedPath) = await UploadHelper.TrySaveFileAsync(
                    _env, receipt, "payment-receipts", Guid.NewGuid());
                if (!okFile)
                {
                    TempData["ErrorMessage"] = fileMsg;
                    return RedirectToAction(nameof(Pay), new { accountId });
                }
                receiptPath = savedPath;
            }

            var (ok, msg, _) = await _requests.SubmitAsync(
                accountId, _currentUser.UserId, amount, method,
                transactionReference, receiptPath, note);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(MyPayments));
        }

        // /Finance/MyPayments — student's submission history
        public async Task<IActionResult> MyPayments()
        {
            var list = await _requests.GetForStudentUserAsync(_currentUser.UserId);
            return View(list);
        }
    }
}
