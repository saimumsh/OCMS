using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Areas.Admin.Models;
using OptimumCoaching.web.Core;
using OptimumCoaching.web.Models;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Areas.Admin.Controllers
{
    public class StudentsController : AdminBaseController
    {
        private readonly IStudentService _studentService;
        private readonly IDepartmentService _departmentService;
        private readonly IBatchService _batchService;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;

        public StudentsController(
            IStudentService studentService,
            IDepartmentService departmentService,
            IBatchService batchService,
            ICurrentUserService currentUser,
            IWebHostEnvironment env)
        {
            _studentService = studentService;
            _departmentService = departmentService;
            _batchService = batchService;
            _currentUser = currentUser;
            _env = env;
        }

        [Authorize(Permissions.Students.ListView)]
        public async Task<IActionResult> Index()
        {
            var students = await _studentService.GetAllAsync();
            return View(MapList(students));
        }

        [Authorize(Permissions.Students.Approve)]
        public async Task<IActionResult> Pending()
        {
            var students = await _studentService.GetAllAsync(status: StudentApprovalStatus.Pending);
            return View(MapList(students));
        }

        [Authorize(Permissions.Students.AddEdit)]
        public async Task<IActionResult> Create()
        {
            var model = new StudentFormViewModel
            {
                ApprovalStatus = StudentApprovalStatus.Approved,
                EnrollmentDate = DateTime.UtcNow.Date
            };
            await PopulateDepartmentsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Students.AddEdit)]
        public async Task<IActionResult> Create(StudentFormViewModel model)
        {
            await PopulateDepartmentsAsync(model);
            NormalizeAcademicRecords(model);
            if (!ModelState.IsValid) return View(model);

            var studentId = Guid.NewGuid();
            var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "students", studentId);
            if (!imgOk)
            {
                ModelState.AddModelError(nameof(model.Image), imgMsg);
                return View(model);
            }

            var student = MapFormToEntity(model, studentId, imgUrl);

            var (success, message, _) = await _studentService.CreateAsync(student, _currentUser.UserId);
            if (!success)
            {
                if (!string.IsNullOrEmpty(imgUrl)) UploadHelper.TryDeleteImage(_env, imgUrl);
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Permissions.Students.AddEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var s = await _studentService.GetByIdAsync(id);
            if (s == null) return NotFound();

            var model = new StudentFormViewModel
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                Address = s.Address,
                GuardianName = s.GuardianName,
                GuardianPhone = s.GuardianPhone,
                EnrollmentDate = s.EnrollmentDate,
                DepartmentId = s.DepartmentId,
                BatchId = s.BatchId,
                Notes = s.Notes,
                ImageUrl = s.ImageUrl,
                ApprovalStatus = s.ApprovalStatus,
                AcademicRecords = s.AcademicRecords
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new AcademicRecordInput
                    {
                        Id = r.Id,
                        ExaminationName = r.ExaminationName,
                        PassingYear = r.PassingYear,
                        Group = r.Group,
                        Result = r.Result,
                        Institution = r.Institution
                    }).ToList()
            };
            await PopulateDepartmentsAsync(model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Students.AddEdit)]
        public async Task<IActionResult> Edit(StudentFormViewModel model)
        {
            await PopulateDepartmentsAsync(model);
            NormalizeAcademicRecords(model);
            if (!ModelState.IsValid) return View(model);

            var existing = await _studentService.GetByIdAsync(model.Id);
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
                var (imgOk, imgMsg, imgUrl) = await UploadHelper.TrySaveImageAsync(_env, model.Image, "students", model.Id);
                if (!imgOk)
                {
                    ModelState.AddModelError(nameof(model.Image), imgMsg);
                    model.ImageUrl = existing.ImageUrl;
                    return View(model);
                }
                newImageUrl = imgUrl;
                oldImageToRemove = existing.ImageUrl;
            }

            var update = MapFormToEntity(model, model.Id, newImageUrl);

            var (success, message) = await _studentService.UpdateAsync(update, _currentUser.UserId);
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
        [Authorize(Permissions.Students.Approve)]
        public async Task<IActionResult> Approve(Guid id)
        {
            var (success, message) = await _studentService.ApproveAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Pending));
        }

        [Authorize(Permissions.Students.Approve)]
        public async Task<IActionResult> Reject(Guid id)
        {
            var s = await _studentService.GetByIdAsync(id);
            if (s == null) return NotFound();
            return View(new RejectStudentViewModel { Id = s.Id, FullName = s.FullName });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Students.Approve)]
        public async Task<IActionResult> Reject(RejectStudentViewModel model)
        {
            var (success, message) = await _studentService.RejectAsync(model.Id, model.Reason, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Permissions.Students.Delete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _studentService.DeleteAsync(id, _currentUser.UserId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private static List<StudentListItem> MapList(IList<Student> students) =>
            students.Select(s => new StudentListItem
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                ApprovalStatus = s.ApprovalStatus,
                IsActive = s.IsActive,
                ImageUrl = s.ImageUrl,
                Created = s.Created,
                GuardianName = s.GuardianName ?? s.Guardian?.FullName
            }).ToList();

        private static Student MapFormToEntity(StudentFormViewModel m, Guid id, string? imageUrl)
        {
            return new Student
            {
                Id = id,
                FullName = m.FullName,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber,
                DateOfBirth = m.DateOfBirth,
                Gender = m.Gender,
                Address = m.Address,
                GuardianName = m.GuardianName,
                GuardianPhone = m.GuardianPhone,
                EnrollmentDate = m.EnrollmentDate,
                DepartmentId = m.DepartmentId,
                BatchId = m.BatchId,
                Notes = m.Notes,
                ImageUrl = imageUrl,
                ApprovalStatus = m.ApprovalStatus,
                AcademicRecords = m.AcademicRecords.Select(r => new StudentAcademicRecord
                {
                    ExaminationName = r.ExaminationName.Trim(),
                    PassingYear = r.PassingYear ?? 0,
                    Group = string.IsNullOrWhiteSpace(r.Group) ? null : r.Group.Trim(),
                    Result = string.IsNullOrWhiteSpace(r.Result) ? null : r.Result.Trim(),
                    Institution = string.IsNullOrWhiteSpace(r.Institution) ? null : r.Institution.Trim()
                }).ToList()
            };
        }

        private async Task PopulateDepartmentsAsync(StudentFormViewModel model)
        {
            var depts = await _departmentService.GetAllAsync();
            model.Departments = depts
                .Where(d => d.IsActive)
                .Select(d => new DepartmentOption { Id = d.Id, Name = d.Name, Stream = d.Stream })
                .ToList();

            var batches = await _batchService.GetAllAsync();
            model.BatchOptions = batches
                .Where(b => b.IsActive)
                .Select(b => new BatchOption { Id = b.Id, Name = b.Name, DepartmentId = b.DepartmentId })
                .ToList();
        }

        private void NormalizeAcademicRecords(StudentFormViewModel model)
        {
            model.AcademicRecords = model.AcademicRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.ExaminationName) || r.PassingYear.HasValue
                    || !string.IsNullOrWhiteSpace(r.Group) || !string.IsNullOrWhiteSpace(r.Result)
                    || !string.IsNullOrWhiteSpace(r.Institution))
                .ToList();

            if (model.AcademicRecords.Count == 0)
                ModelState.AddModelError(nameof(model.AcademicRecords),
                    "Please add at least one academic record");
        }
    }
}
