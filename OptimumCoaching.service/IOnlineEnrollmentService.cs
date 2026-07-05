using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public class OnlineEnrollmentSummary
    {
        public CourseEnrollment Enrollment { get; set; } = null!;
        public Batch Batch { get; set; } = null!;
        public StudentFeeAccount? FeeAccount { get; set; }
        public int LessonsCount { get; set; }
        public int LessonsCompleted { get; set; }
        public int LessonProgressPercent =>
            LessonsCount == 0 ? 0 : (int)Math.Round(LessonsCompleted * 100.0 / LessonsCount);
    }

    public interface IOnlineEnrollmentService
    {
        // -------- Admin-side --------
        Task<IList<Batch>> GetOnlineBatchesAsync();
        Task<IList<CourseEnrollment>> GetEnrollmentsForBatchAsync(Guid batchId);
        Task<(bool Success, string Message)> CancelAsync(Guid enrollmentId, string? note, Guid? actorId);

        // -------- Student-side --------
        Task<IList<OnlineEnrollmentSummary>> GetForStudentAsync(Guid studentId);
        Task<OnlineEnrollmentSummary?> GetByIdAsync(Guid enrollmentId, Guid studentId);

        // Idempotent: returns existing active enrollment if the student is
        // already enrolled. Creates the row + a fee account otherwise.
        Task<(bool Success, string Message, CourseEnrollment? Enrollment)> EnrollAsync(
            Guid studentId, Guid batchId, Guid? actorId);
    }
}
