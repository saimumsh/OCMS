using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class ExamService : IExamService
    {
        private readonly IRepository<Exam> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public ExamService(IRepository<Exam> repo, IUnitOfWork uow, ApplicationDbContext db)
        {
            _repo = repo; _uow = uow; _db = db;
        }

        public async Task<IList<Exam>> GetAllAsync(Guid? batchId = null, ExamStatus? status = null)
        {
            IQueryable<Exam> q = _db.Exams
                .Include(e => e.Batch).ThenInclude(b => b.Department)
                .Where(e => !e.IsDeleted);
            if (batchId.HasValue) q = q.Where(e => e.BatchId == batchId.Value);
            if (status.HasValue)  q = q.Where(e => e.Status == status.Value);
            return await q.OrderByDescending(e => e.ExamDate).ToListAsync();
        }

        public async Task<IList<Exam>> GetUpcomingForStudentAsync(Guid studentId, int take = 5)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student?.BatchId == null) return new List<Exam>();
            var today = DateTime.UtcNow.Date;
            return await _db.Exams
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Where(e => !e.IsDeleted && e.BatchId == student.BatchId
                            && e.Status != ExamStatus.Cancelled
                            && e.ExamDate >= today)
                .OrderBy(e => e.ExamDate)
                .Take(take)
                .ToListAsync();
        }

        public Task<IList<Exam>> GetForTeacherAsync(Guid teacherId, ExamStatus? status = null)
        {
            // Lead OR co-teacher counts as "for this teacher".
            var coTaughtIds = _db.BatchTeachers
                .Where(bt => !bt.IsDeleted && bt.TeacherId == teacherId)
                .Select(bt => bt.BatchId);

            var q = _db.Exams
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Where(e => !e.IsDeleted &&
                            (e.Batch.TeacherId == teacherId || coTaughtIds.Contains(e.BatchId)));
            if (status.HasValue) q = q.Where(e => e.Status == status.Value);
            return q.OrderByDescending(e => e.ExamDate).ToListAsync()
                .ContinueWith(t => (IList<Exam>)t.Result);
        }

        public Task<Exam?> GetByIdAsync(Guid id) =>
            _db.Exams
                .Include(e => e.Batch).ThenInclude(b => b.Department)
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Include(e => e.Batch).ThenInclude(b => b.Teacher)
                .FirstOrDefaultAsync(e => e.Id == id);

        public async Task<(bool Success, string Message, Exam? Exam)> CreateAsync(
            Exam exam, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(exam.Title))
                return (false, "Title is required", null);
            if (exam.BatchId == Guid.Empty)
                return (false, "Batch is required", null);
            if (exam.TotalMarks <= 0)
                return (false, "Total marks must be greater than 0", null);
            if (exam.PassMarks > exam.TotalMarks)
                return (false, "Pass marks cannot exceed total marks", null);

            exam.Id = exam.Id == Guid.Empty ? Guid.NewGuid() : exam.Id;
            exam.IsActive = true;
            exam.IsDeleted = false;
            exam.Status = ExamStatus.Draft;
            exam.Created = DateTime.UtcNow;
            exam.CreatedBy = createdBy;
            await _repo.AddAsync(exam);
            await _uow.CompleteAsync();
            return (true, "Exam created", exam);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(
            Exam exam, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(exam.Id);
            if (existing == null || existing.IsDeleted) return (false, "Exam not found");
            if (exam.PassMarks > exam.TotalMarks)
                return (false, "Pass marks cannot exceed total marks");

            existing.Title = exam.Title;
            existing.Type = exam.Type;
            existing.ExamDate = exam.ExamDate;
            existing.TotalMarks = exam.TotalMarks;
            existing.PassMarks = exam.PassMarks;
            existing.DurationMinutes = exam.DurationMinutes;
            existing.Syllabus = exam.Syllabus;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;

            await _uow.CompleteAsync();
            return (true, "Exam updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Exam not found");

            var hasResults = await _db.ExamResults.AnyAsync(r => r.ExamId == id && !r.IsDeleted);
            if (hasResults) return (false, "Cannot delete — results have been entered for this exam");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;
            await _uow.CompleteAsync();
            return (true, "Exam removed");
        }

        public async Task<(bool Success, string Message)> PublishAsync(Guid id, Guid? actorId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return (false, "Exam not found");
            existing.Status = ExamStatus.Published;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Exam published");
        }
    }

    public class ExamResultService : IExamResultService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public ExamResultService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public async Task<IList<ExamResultRow>> BuildGradingGridAsync(Guid examId)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return new List<ExamResultRow>();

            var students = await _db.Students
                .Where(s => !s.IsDeleted && s.BatchId == exam.BatchId)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, s.FullName, s.StudentCode })
                .ToListAsync();

            var existing = await _db.ExamResults
                .Where(r => r.ExamId == examId && !r.IsDeleted)
                .ToDictionaryAsync(r => r.StudentId);

            return students.Select(s =>
            {
                existing.TryGetValue(s.Id, out var r);
                return new ExamResultRow
                {
                    ResultId = r?.Id,
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    StudentCode = s.StudentCode,
                    IsPresent = r?.IsPresent ?? true,
                    MarksObtained = r?.MarksObtained,
                    Remarks = r?.Remarks,
                    Status = r?.Status ?? ResultStatus.Draft
                };
            }).ToList();
        }

        public async Task<(bool Success, string Message)> SaveDraftAsync(
            Guid examId, IList<ExamResultRow> rows, Guid? actorId)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return (false, "Exam not found");

            var existing = await _db.ExamResults
                .Where(r => r.ExamId == examId)
                .ToDictionaryAsync(r => r.StudentId);

            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                if (row.IsPresent && row.MarksObtained.HasValue &&
                    (row.MarksObtained < 0 || row.MarksObtained > exam.TotalMarks))
                {
                    return (false, $"Marks must be between 0 and {exam.TotalMarks} for {row.StudentName}");
                }

                if (existing.TryGetValue(row.StudentId, out var er))
                {
                    if (er.Status == ResultStatus.Published) continue; // don't overwrite published
                    er.IsPresent = row.IsPresent;
                    er.MarksObtained = row.IsPresent ? row.MarksObtained : null;
                    er.Remarks = row.Remarks;
                    er.LastModified = now;
                    er.LastModifiedBy = actorId;
                }
                else
                {
                    _db.ExamResults.Add(new ExamResult
                    {
                        Id = Guid.NewGuid(),
                        ExamId = examId,
                        StudentId = row.StudentId,
                        IsPresent = row.IsPresent,
                        MarksObtained = row.IsPresent ? row.MarksObtained : null,
                        Remarks = row.Remarks,
                        Status = ResultStatus.Draft,
                        IsActive = true,
                        Created = now,
                        CreatedBy = actorId
                    });
                }
            }

            await _uow.CompleteAsync();
            return (true, "Draft saved");
        }

        public async Task<(bool Success, string Message)> PublishAllAsync(Guid examId, Guid? actorId)
        {
            var rows = await _db.ExamResults
                .Where(r => r.ExamId == examId && !r.IsDeleted && r.Status != ResultStatus.Published)
                .ToListAsync();
            if (!rows.Any()) return (false, "No draft results to publish");

            var now = DateTime.UtcNow;
            foreach (var r in rows)
            {
                r.Status = ResultStatus.Published;
                r.PublishedAt = now;
                r.PublishedByUserId = actorId;
                r.LastModified = now;
                r.LastModifiedBy = actorId;
            }
            await _uow.CompleteAsync();
            return (true, $"Published {rows.Count} result(s)");
        }

        public Task<IList<ExamResult>> GetForStudentAsync(Guid studentId, bool publishedOnly = true) =>
            _db.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e.Batch).ThenInclude(b => b.Subject)
                .Where(r => !r.IsDeleted && r.StudentId == studentId &&
                            (!publishedOnly || r.Status == ResultStatus.Published))
                .OrderByDescending(r => r.Exam.ExamDate)
                .ToListAsync()
                .ContinueWith(t => (IList<ExamResult>)t.Result);

        public Task<IList<ExamResult>> GetForExamAsync(Guid examId) =>
            _db.ExamResults
                .Include(r => r.Student)
                .Where(r => !r.IsDeleted && r.ExamId == examId)
                .OrderBy(r => r.Student.FullName)
                .ToListAsync()
                .ContinueWith(t => (IList<ExamResult>)t.Result);
    }
}
