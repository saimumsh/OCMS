using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class OnlineEnrollmentService : IOnlineEnrollmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFeeService _fees;
        private readonly IUnitOfWork _uow;

        public OnlineEnrollmentService(
            ApplicationDbContext db, IFeeService fees, IUnitOfWork uow)
        {
            _db = db; _fees = fees; _uow = uow;
        }

        public Task<IList<Batch>> GetOnlineBatchesAsync() =>
            _db.Batches
                .Include(b => b.Department)
                .Include(b => b.Subject)
                .Include(b => b.Teacher)
                .Where(b => !b.IsDeleted &&
                            (b.DeliveryMode == DeliveryMode.Online || b.DeliveryMode == DeliveryMode.Hybrid))
                .OrderByDescending(b => b.Created)
                .ToListAsync()
                .ContinueWith(t => (IList<Batch>)t.Result);

        public Task<IList<CourseEnrollment>> GetEnrollmentsForBatchAsync(Guid batchId) =>
            _db.CourseEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.Department)
                .Where(e => !e.IsDeleted && e.BatchId == batchId)
                .OrderByDescending(e => e.EnrolledOn)
                .ToListAsync()
                .ContinueWith(t => (IList<CourseEnrollment>)t.Result);

        public async Task<(bool Success, string Message)> CancelAsync(Guid enrollmentId, string? note, Guid? actorId)
        {
            var enrollment = await _db.CourseEnrollments
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted);
            if (enrollment == null) return (false, "Enrollment not found");

            enrollment.Status = EnrollmentStatus.Cancelled;
            enrollment.Note = string.IsNullOrWhiteSpace(note) ? enrollment.Note : note.Trim();
            enrollment.LastModified = DateTime.UtcNow;
            enrollment.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Enrollment cancelled");
        }

        public async Task<IList<OnlineEnrollmentSummary>> GetForStudentAsync(Guid studentId)
        {
            var enrollments = await _db.CourseEnrollments
                .Include(e => e.Batch).ThenInclude(b => b.Department)
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Include(e => e.Batch).ThenInclude(b => b.Teacher)
                .Where(e => !e.IsDeleted && e.StudentId == studentId
                            && e.Status != EnrollmentStatus.Cancelled)
                .OrderByDescending(e => e.EnrolledOn)
                .ToListAsync();

            var batchIds = enrollments.Select(e => e.BatchId).ToList();

            var feeAccounts = await _db.StudentFeeAccounts
                .Where(a => !a.IsDeleted && a.StudentId == studentId && batchIds.Contains(a.BatchId))
                .ToDictionaryAsync(a => a.BatchId);

            var lessonCounts = await _db.CourseLessons
                .Where(l => !l.IsDeleted && batchIds.Contains(l.BatchId))
                .GroupBy(l => l.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            var completedCounts = await _db.StudentLessonProgresses
                .Where(p => !p.IsDeleted && p.StudentId == studentId && p.CompletedAt != null)
                .Join(_db.CourseLessons.Where(l => !l.IsDeleted && batchIds.Contains(l.BatchId)),
                      p => p.LessonId, l => l.Id, (p, l) => l.BatchId)
                .GroupBy(b => b)
                .Select(g => new { BatchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BatchId, x => x.Count);

            return enrollments.Select(e => new OnlineEnrollmentSummary
            {
                Enrollment = e,
                Batch = e.Batch,
                FeeAccount = feeAccounts.TryGetValue(e.BatchId, out var fa) ? fa : null,
                LessonsCount = lessonCounts.TryGetValue(e.BatchId, out var lc) ? lc : 0,
                LessonsCompleted = completedCounts.TryGetValue(e.BatchId, out var cc) ? cc : 0
            }).ToList();
        }

        public async Task<OnlineEnrollmentSummary?> GetByIdAsync(Guid enrollmentId, Guid studentId)
        {
            var enrollment = await _db.CourseEnrollments
                .Include(e => e.Batch).ThenInclude(b => b.Department)
                .Include(e => e.Batch).ThenInclude(b => b.Subject)
                .Include(e => e.Batch).ThenInclude(b => b.Teacher)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted
                                         && e.StudentId == studentId);
            if (enrollment == null) return null;

            var fa = await _db.StudentFeeAccounts.FirstOrDefaultAsync(a =>
                !a.IsDeleted && a.StudentId == studentId && a.BatchId == enrollment.BatchId);

            var lessonsCount = await _db.CourseLessons
                .CountAsync(l => !l.IsDeleted && l.BatchId == enrollment.BatchId);
            var completed = await _db.StudentLessonProgresses
                .Where(p => !p.IsDeleted && p.StudentId == studentId && p.CompletedAt != null)
                .Join(_db.CourseLessons.Where(l => !l.IsDeleted && l.BatchId == enrollment.BatchId),
                      p => p.LessonId, l => l.Id, (p, l) => 1)
                .CountAsync();

            return new OnlineEnrollmentSummary
            {
                Enrollment = enrollment,
                Batch = enrollment.Batch,
                FeeAccount = fa,
                LessonsCount = lessonsCount,
                LessonsCompleted = completed
            };
        }

        public async Task<(bool Success, string Message, CourseEnrollment? Enrollment)> EnrollAsync(
            Guid studentId, Guid batchId, Guid? actorId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null) return (false, "Student profile not found", null);

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return (false, "Course not found", null);
            if (!batch.IsPublishedForEnrollment) return (false, "This course is not open for enrollment", null);

            var existing = await _db.CourseEnrollments
                .FirstOrDefaultAsync(e => !e.IsDeleted && e.StudentId == studentId && e.BatchId == batchId);
            if (existing != null)
            {
                if (existing.Status == EnrollmentStatus.Cancelled)
                {
                    existing.Status = EnrollmentStatus.Active;
                    existing.EnrolledOn = DateTime.UtcNow;
                    existing.LastModified = DateTime.UtcNow;
                    existing.LastModifiedBy = actorId;
                    await _uow.CompleteAsync();
                }
                await _fees.EnsureAccountAsync(studentId, batchId, actorId);
                return (true, "You are already enrolled in this course", existing);
            }

            if (batch.Capacity.HasValue)
            {
                var current = await _db.CourseEnrollments
                    .CountAsync(e => !e.IsDeleted && e.BatchId == batchId
                                     && e.Status == EnrollmentStatus.Active);
                if (current >= batch.Capacity.Value) return (false, "This course is full", null);
            }

            // Snapshot the price the student actually sees today.
            var today = DateTime.UtcNow.Date;
            var price = (batch.OfferedPrice.HasValue && batch.OfferedPrice.Value < batch.CourseFee
                         && (!batch.OfferEndsAt.HasValue || batch.OfferEndsAt.Value.Date >= today))
                ? batch.OfferedPrice!.Value
                : batch.CourseFee;

            var now = DateTime.UtcNow;
            var enrollment = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                BatchId = batchId,
                EnrolledOn = now,
                Status = EnrollmentStatus.Active,
                PriceAtEnrollment = price,
                IsActive = true,
                Created = now,
                CreatedBy = actorId
            };
            _db.CourseEnrollments.Add(enrollment);
            await _uow.CompleteAsync();

            await _fees.EnsureAccountAsync(studentId, batchId, actorId);
            return (true, "Enrolled — please complete the payment to confirm your seat", enrollment);
        }
    }
}
