using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface ITeacherFeedbackService
    {
        // ---- Reviews ----
        Task<IList<TeacherReview>> GetReviewsForTeacherAsync(Guid teacherId);
        Task<TeacherRatingSummary> GetRatingSummaryAsync(Guid teacherId);

        // Upserts a review (one per student-teacher pair). Returns the saved row.
        Task<(bool Success, string Message, TeacherReview? Review)> UpsertReviewAsync(
            Guid studentId, Guid teacherId, Guid? batchId, int rating, string? comment);

        // ---- Reports ----
        Task<IList<TeacherReport>> GetReportsAsync(ReportStatus? status = null);
        Task<TeacherReport?> GetReportByIdAsync(Guid id);
        Task<(bool Success, string Message, TeacherReport? Report)> CreateReportAsync(
            Guid studentId, Guid teacherId, Guid? batchId, ReportCategory category, string description);

        Task<(bool Success, string Message)> UpdateReportStatusAsync(
            Guid reportId, ReportStatus newStatus, string? adminNote, Guid? handledBy);
    }

    public class TeacherRatingSummary
    {
        public int Count { get; set; }
        public double AverageRating { get; set; }
        public int Star1 { get; set; }
        public int Star2 { get; set; }
        public int Star3 { get; set; }
        public int Star4 { get; set; }
        public int Star5 { get; set; }
    }
}
