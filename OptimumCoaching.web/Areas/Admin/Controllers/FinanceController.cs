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
        private readonly IPaymentSettingsService _paySettings;
        private readonly IFeePaymentRequestService _payRequests;
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public FinanceController(
            IFeeService fees,
            ISalaryService salaries,
            IPaymentSettingsService paySettings,
            IFeePaymentRequestService payRequests,
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _fees = fees; _salaries = salaries; _paySettings = paySettings;
            _payRequests = payRequests;
            _db = db; _currentUser = currentUser;
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
            ViewBag.EnabledMethods = await _paySettings.GetEnabledMethodsAsync();
            var accounts = await _fees.GetForBatchAsync(batchId.Value);
            return View(accounts);
        }

        // /Admin/Finance/Students — searchable per-student aggregate list
        [Authorize(Permissions.Finance.ListView)]
        public async Task<IActionResult> Students(string? q = null)
        {
            // Aggregate fee accounts per student so each row shows the student's
            // total fee, total paid, total balance across all their batches.
            var rowsQuery =
                from a in _db.StudentFeeAccounts.Where(x => !x.IsDeleted)
                join s in _db.Students.Where(x => !x.IsDeleted) on a.StudentId equals s.Id
                group new { a, s } by new { s.Id, s.FullName, s.StudentCode, s.Email, s.PhoneNumber } into g
                select new StudentFeeRow
                {
                    StudentId = g.Key.Id,
                    FullName = g.Key.FullName,
                    StudentCode = g.Key.StudentCode,
                    Email = g.Key.Email,
                    PhoneNumber = g.Key.PhoneNumber,
                    AccountCount = g.Count(),
                    FinalFee = g.Sum(x => x.a.FinalFee),
                    AmountPaid = g.Sum(x => x.a.AmountPaid),
                    DiscountAmount = g.Sum(x => x.a.DiscountAmount),
                    UnpaidAccounts = g.Count(x =>
                        x.a.Status != FeeAccountStatus.PaidInFull &&
                        x.a.Status != FeeAccountStatus.Waived)
                };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                rowsQuery = rowsQuery.Where(r =>
                    r.FullName.ToLower().Contains(term) ||
                    (r.StudentCode != null && r.StudentCode.ToLower().Contains(term)) ||
                    (r.Email != null && r.Email.ToLower().Contains(term)) ||
                    (r.PhoneNumber != null && r.PhoneNumber.Contains(term)));
            }

            var rows = await rowsQuery.OrderBy(r => r.FullName).ToListAsync();
            ViewBag.Query = q;
            return View(rows);
        }

        // /Admin/Finance/StudentLedger/{id} — full ledger for one student
        [Authorize(Permissions.Finance.ListView)]
        public async Task<IActionResult> StudentLedger(Guid id)
        {
            var student = await _db.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (student == null) return NotFound();

            var accounts = await _fees.GetForStudentAsync(id);
            ViewBag.Student = student;
            ViewBag.EnabledMethods = await _paySettings.GetEnabledMethodsAsync();
            return View(accounts);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordPayment)]
        public async Task<IActionResult> RecordPayment(
            Guid accountId, decimal amount, DateTime paidOn,
            PaymentMethod method, string? receiptNumber, string? note,
            Guid? batchId, Guid? studentId)
        {
            var (ok, msg, _) = await _fees.RecordPaymentAsync(
                accountId, amount, paidOn, method, receiptNumber, note, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            // Round-trip back to whichever screen called us.
            if (studentId.HasValue) return RedirectToAction(nameof(StudentLedger), new { id = studentId });
            return RedirectToAction(nameof(StudentFees), new { batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.ManageFees)]
        public async Task<IActionResult> ApplyDiscount(
            Guid accountId, decimal discountAmount, string? reason,
            Guid? batchId, Guid? studentId)
        {
            var (ok, msg) = await _fees.ApplyDiscountAsync(
                accountId, discountAmount, reason, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            if (studentId.HasValue) return RedirectToAction(nameof(StudentLedger), new { id = studentId });
            return RedirectToAction(nameof(StudentFees), new { batchId });
        }

        // ---- Dues inbox: every overdue fee account institution-wide ----

        [Authorize(Permissions.Finance.ListView)]
        public async Task<IActionResult> Dues([FromServices] IFeeDueAlertService dueService)
        {
            var rows = await dueService.GetAllOverdueAsync();
            return View(rows);
        }

        // ---- Pending student-submitted payment requests ----

        [Authorize(Permissions.Finance.RecordPayment)]
        public async Task<IActionResult> PendingRequests()
        {
            var list = await _payRequests.GetPendingAsync();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordPayment)]
        public async Task<IActionResult> ApproveRequest(Guid id)
        {
            var (ok, msg) = await _payRequests.ApproveAsync(id, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(PendingRequests));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Finance.RecordPayment)]
        public async Task<IActionResult> RejectRequest(Guid id, string? reason)
        {
            var (ok, msg) = await _payRequests.RejectAsync(id, reason, _currentUser.UserId);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = msg;
            return RedirectToAction(nameof(PendingRequests));
        }

        // Row projection used by the per-student list.
        public class StudentFeeRow
        {
            public Guid StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string? StudentCode { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public int AccountCount { get; set; }
            public decimal FinalFee { get; set; }
            public decimal AmountPaid { get; set; }
            public decimal DiscountAmount { get; set; }
            public int UnpaidAccounts { get; set; }
            public decimal Balance => FinalFee - AmountPaid;
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
