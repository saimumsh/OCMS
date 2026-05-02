using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Models;
using OptimumCoaching.web.Services;
using System.Linq;

namespace OptimumCoaching.web.Controllers
{
    [Authorize]
    public class StudentRegistrationController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IGuardianService _guardianService;
        private readonly ITeacherService _teacherService;
        private readonly IDepartmentService _departmentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public StudentRegistrationController(
            IStudentService studentService,
            IGuardianService guardianService,
            ITeacherService teacherService,
            IDepartmentService departmentService,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _studentService = studentService;
            _guardianService = guardianService;
            _teacherService = teacherService;
            _departmentService = departmentService;
            _userManager = userManager;
            _currentUser = currentUser;
            _env = env;
        }

        // Two-card profile-completion landing page for users who have only the User
        // role and haven't yet registered as Student / Guardian.
        public async Task<IActionResult> Index()
        {
            var existingStudent = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (existingStudent != null)
                return RedirectToAction(nameof(Status));

            var existingGuardian = await _guardianService.GetByUserIdAsync(_currentUser.UserId);
            if (existingGuardian != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        public async Task<IActionResult> Register()
        {
            var existing = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (existing != null) return RedirectToAction(nameof(Status));

            var user = await _userManager.GetUserAsync(User);
            var model = new StudentRegistrationViewModel
            {
                FullName = user?.FullName ?? string.Empty,
                Email = user?.Email
            };
            await PopulateDepartmentsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentRegistrationViewModel model)
        {
            await PopulateDepartmentsAsync(model);

            // Drop blank rows the user added but didn't fill in.
            model.AcademicRecords = model.AcademicRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.ExaminationName) || r.PassingYear.HasValue
                    || !string.IsNullOrWhiteSpace(r.Group) || !string.IsNullOrWhiteSpace(r.Result)
                    || !string.IsNullOrWhiteSpace(r.Institution))
                .ToList();

            if (model.AcademicRecords.Count == 0)
                ModelState.AddModelError(nameof(model.AcademicRecords),
                    "Please add at least one academic record");

            if (!ModelState.IsValid) return View(model);

            var existing = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (existing != null) return RedirectToAction(nameof(Status));

            var studentId = Guid.NewGuid();
            var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "students", studentId);
            if (!imgOk)
            {
                ModelState.AddModelError(nameof(model.Image), imgMsg);
                return View(model);
            }

            var student = new Student
            {
                Id = studentId,
                UserId = _currentUser.UserId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                GuardianName = model.GuardianName,
                GuardianPhone = model.GuardianPhone,
                DepartmentId = model.DepartmentId,
                ImageUrl = imgUrl,
                ApprovalStatus = StudentApprovalStatus.Pending,
                AcademicRecords = model.AcademicRecords.Select(r => new StudentAcademicRecord
                {
                    ExaminationName = r.ExaminationName.Trim(),
                    PassingYear = r.PassingYear ?? 0,
                    Group = string.IsNullOrWhiteSpace(r.Group) ? null : r.Group.Trim(),
                    Result = string.IsNullOrWhiteSpace(r.Result) ? null : r.Result.Trim(),
                    Institution = string.IsNullOrWhiteSpace(r.Institution) ? null : r.Institution.Trim()
                }).ToList()
            };

            var (success, message, _) = await _studentService.CreateAsync(student, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            // Mirror Full Name + photo onto the ApplicationUser so the navbar reflects it.
            var u = await _userManager.GetUserAsync(User);
            if (u != null)
            {
                u.FullName = model.FullName;
                if (!string.IsNullOrEmpty(imgUrl)) u.ImageUrl = imgUrl;
                await _userManager.UpdateAsync(u);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Status));
        }

        public async Task<IActionResult> Status()
        {
            var student = await _studentService.GetByUserIdAsync(_currentUser.UserId);
            if (student == null) return RedirectToAction(nameof(Index));
            return View(student);
        }

        private async Task PopulateDepartmentsAsync(StudentRegistrationViewModel model)
        {
            var depts = await _departmentService.GetAllAsync();
            model.Departments = depts
                .Where(d => d.IsActive)
                .Select(d => new DepartmentOption { Id = d.Id, Name = d.Name, Stream = d.Stream })
                .ToList();
        }
    }
}
