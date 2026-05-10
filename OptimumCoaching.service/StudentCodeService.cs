using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class StudentCodeService : IStudentCodeService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public StudentCodeService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db;
            _uow = uow;
        }

        public async Task<(bool Success, string Message, string? Code)> AssignAsync(
            Guid studentId, bool force = false)
        {
            var student = await _db.Students
                .Include(s => s.Department)
                .Include(s => s.Batch)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return (false, "Student not found", null);
            if (!force && !string.IsNullOrWhiteSpace(student.StudentCode))
                return (true, "Student already has a code", student.StudentCode);

            if (student.ApprovalStatus != StudentApprovalStatus.Approved)
                return (false, "Student must be approved first", null);
            if (student.Department == null)
                return (false, "Department is required", null);
            if (student.Batch == null)
                return (false, "Batch is required", null);
            if (string.IsNullOrWhiteSpace(student.Session))
                return (false, "Session is required (e.g. \"2026\" or \"2025-26\")", null);

            var deptCode  = !string.IsNullOrWhiteSpace(student.Department.Code)
                ? student.Department.Code!
                : Sanitize(student.Department.Name, 3);
            var batchCode = !string.IsNullOrWhiteSpace(student.Batch.Code)
                ? student.Batch.Code!
                : Sanitize(student.Batch.Name, 4);

            student.StudentCode = await PreviewNextCodeAsync(deptCode, student.Session!, batchCode);
            student.LastModified = DateTime.UtcNow;
            await _uow.CompleteAsync();
            return (true, "Code assigned", student.StudentCode);
        }

        public async Task<string> PreviewNextCodeAsync(string deptCode, string session, string batchCode)
        {
            // Sequence is per (Dept, Session, Batch) — the most natural cohort.
            var prefix = $"{deptCode}-{session}-{batchCode}-";
            var existing = await _db.Students
                .Where(s => s.StudentCode != null && s.StudentCode.StartsWith(prefix))
                .Select(s => s.StudentCode!)
                .ToListAsync();

            int next = 1;
            foreach (var code in existing)
            {
                var tail = code[prefix.Length..];
                if (int.TryParse(tail, out var n) && n >= next) next = n + 1;
            }
            return $"{prefix}{next:D4}";
        }

        private static string Sanitize(string s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return "X";
            var clean = new string(s.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return clean.Length > max ? clean[..max] : clean;
        }
    }
}
