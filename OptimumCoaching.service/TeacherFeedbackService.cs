using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class TeacherFeedbackService : ITeacherFeedbackService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public TeacherFeedbackService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        // ---- Reviews ----

        public Task<IList<TeacherReview>> GetReviewsForTeacherAsync(Guid teacherId) =>
            _db.TeacherReviews
                .Include(r => r.Student)
                .Include(r => r.Batch)
                .Where(r => !r.IsDeleted && r.TeacherId == teacherId)
                .OrderByDescending(r => r.Created)
                .ToListAsync()
                .ContinueWith(t => (IList<TeacherReview>)t.Result);

        public async Task<TeacherRatingSummary> GetRatingSummaryAsync(Guid teacherId)
        {
            var ratings = await _db.TeacherReviews
                .Where(r => !r.IsDeleted && r.TeacherId == teacherId)
                .Select(r => r.Rating)
                .ToListAsync();

            return new TeacherRatingSummary
            {
                Count = ratings.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 2) : 0d,
                Star1 = ratings.Count(r => r == 1),
                Star2 = ratings.Count(r => r == 2),
                Star3 = ratings.Count(r => r == 3),
                Star4 = ratings.Count(r => r == 4),
                Star5 = ratings.Count(r => r == 5),
            };
        }

        public async Task<(bool Success, string Message, TeacherReview? Review)> UpsertReviewAsync(
            Guid studentId, Guid teacherId, Guid? batchId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
                return (false, "Rating must be between 1 and 5", null);

            var existing = await _db.TeacherReviews.FirstOrDefaultAsync(r =>
                !r.IsDeleted && r.StudentId == studentId && r.TeacherId == teacherId);

            var now = DateTime.UtcNow;
            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.BatchId = batchId;
                existing.LastModified = now;
                existing.LastModifiedBy = studentId;
                await _uow.CompleteAsync();
                return (true, "Review updated", existing);
            }

            var review = new TeacherReview
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                TeacherId = teacherId,
                BatchId = batchId,
                Rating = rating,
                Comment = comment,
                IsActive = true,
                Created = now,
                CreatedBy = studentId
            };
            _db.TeacherReviews.Add(review);
            await _uow.CompleteAsync();
            return (true, "Thanks for your review", review);
        }

        // ---- Reports ----

        public Task<IList<TeacherReport>> GetReportsAsync(ReportStatus? status = null)
        {
            var q = _db.TeacherReports
                .Include(r => r.Student)
                .Include(r => r.Teacher)
                .Include(r => r.Batch)
                .Include(r => r.HandledByUser)
                .Where(r => !r.IsDeleted);
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);
            return q.OrderByDescending(r => r.Created).ToListAsync()
                .ContinueWith(t => (IList<TeacherReport>)t.Result);
        }

        public Task<TeacherReport?> GetReportByIdAsync(Guid id) =>
            _db.TeacherReports
                .Include(r => r.Student)
                .Include(r => r.Teacher)
                .Include(r => r.Batch)
                .Include(r => r.HandledByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<(bool Success, string Message, TeacherReport? Report)> CreateReportAsync(
            Guid studentId, Guid teacherId, Guid? batchId,
            ReportCategory category, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return (false, "Description is required", null);

            var report = new TeacherReport
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                TeacherId = teacherId,
                BatchId = batchId,
                Category = category,
                Description = description.Trim(),
                Status = ReportStatus.Open,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = studentId
            };
            _db.TeacherReports.Add(report);
            await _uow.CompleteAsync();
            return (true, "Report submitted to administration", report);
        }

        public async Task<(bool Success, string Message)> UpdateReportStatusAsync(
            Guid reportId, ReportStatus newStatus, string? adminNote, Guid? handledBy)
        {
            var report = await _db.TeacherReports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null || report.IsDeleted) return (false, "Report not found");

            report.Status = newStatus;
            report.AdminNote = adminNote;
            report.HandledByUserId = handledBy;
            report.HandledAt = DateTime.UtcNow;
            report.LastModified = DateTime.UtcNow;
            report.LastModifiedBy = handledBy;
            await _uow.CompleteAsync();
            return (true, "Report updated");
        }
    }
}
