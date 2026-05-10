using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.service;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class FinanceController : AdminBaseController
    {
        private readonly IFeeService _fees;
        private readonly ISalaryService _salaries;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public FinanceController(
            IFeeService fees,
            ISalaryService salaries,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _fees = fees; _salaries = salaries; _db = db; _currentUser = currentUser;
        }

        // /Admin/Finance — quick KPI view
        [Authorize(Permissions.Finance.ListView)]
        public async Task<IActionResult> Index()
        {
            var totalCollected = await _db.FeePayments.Where(p => !p.IsDeleted).SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var totalOutstanding = await _db.StudentFeeAccounts
                .Where(a => !a.IsDeleted && a.Status != FeeAccountStatus.PaidInFull && a.Status != FeeAccountStatus.Waived)
                .SumAsync(a => (decimal?)(a.FinalFee - a.AmountPaid)) ?? 0m;
            var totalSalaries30d = await _db.TeacherSalaryPayments
                .Where(s => !s.IsDeleted && s.PaidOn >= DateTime.UtcNow.AddDays(-30))
                .SumAsync(s => (decimal?)s.Amount) ?? 0m;
            var unpaidStudents = await _db.StudentFeeAccounts
                .CountAsync(a => !a.IsDeleted && a.Status != FeeAccountStatus.PaidInFull && a.Status != FeeAccountStatus.Waived);

            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalOutstanding = totalOutstanding;
            ViewBag.TotalSalaries30d = totalSalaries30d;
            ViewBag.UnpaidStudents = unpaidStudents;
            return View();
        }

        // /Admin/Finance/StudentFees?batchId=...
        [Authorize(Permissions.Finance.ListView)]
        public async Task<IActionResult> StudentFees(Guid? batchId = null)
        {
            ViewBag.BatchOptions = await _db.Batches
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
            ViewBag.ActiveBatchId = batchId;

            if (!batchId.HasValue) return View(new List<StudentFeeAccount>());

            ViewBag.Batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId);
            var accounts = await _fees.GetForBatchAsync(batchId.Value);
            return View(accounts);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordPayment)]
        public async Task<IActionResult> RecordPayment(
            Guid accountId, decimal amount, DateTime paidOn,
            PaymentMethod method, string? receiptNumber, string? note, Guid? batchId)
        {
            var (ok, msg, _) = await _fees.RecordPaymentAsync(
                accountId, amount, paidOn, method, receiptNumber, note, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(StudentFees), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> ApplyDiscount(
            Guid accountId, decimal discountAmount, string? reason, Guid? batchId)
        {
            var (ok, msg) = await _fees.ApplyDiscountAsync(
                accountId, discountAmount, reason, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(StudentFees), new { batchId });
        }

        // ---- Salaries ----

        [Authorize(Permissions.Finance.ViewSalaries)]
        public async Task<IActionResult> Salaries(int? year = null, int? month = null)
        {
            var list = await _salaries.GetAllAsync(year, month);
            ViewBag.TeacherOptions = await _db.Teachers
                .Where(t => !t.IsDeleted && t.IsActive)
                .OrderBy(t => t.FullName)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName })
                .ToListAsync();
            ViewBag.ActiveYear = year ?? DateTime.UtcNow.Year;
            ViewBag.ActiveMonth = month;
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordSalary)]
        public async Task<IActionResult> RecordSalary(
            Guid teacherId, DateTime periodMonth, decimal amount, DateTime paidOn,
            PaymentMethod method, string? reference, string? note)
        {
            var (ok, msg, _) = await _salaries.RecordAsync(
                teacherId, periodMonth, amount, paidOn, method, reference, note, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Salaries));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordSalary)]
        public async Task<IActionResult> DeleteSalary(Guid id)
        {
            var (ok, msg) = await _salaries.DeleteAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(Salaries));
        }
    }
}
