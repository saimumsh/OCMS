using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface ICourseLessonService
    {
        Task<IList<CourseLesson>> GetForBatchAsync(Guid batchId, bool publishedOnly = false);
        Task<CourseLesson?> GetByIdAsync(Guid id);

        // Per-student view returning each lesson hydrated with the student's
        // completion flag and any first-opened timestamp.
        Task<IList<LessonWithProgress>> GetForStudentAsync(Guid studentId, Guid batchId);

        Task<(bool Success, string Message, CourseLesson? Lesson)> CreateAsync(CourseLesson lesson, Guid? actorId);
        Task<(bool Success, string Message)> UpdateAsync(CourseLesson lesson, Guid? actorId);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);
        Task<(bool Success, string Message)> PublishAsync(Guid id, bool publish, Guid? actorId);

        // Student-side: stamp first-opened (idempotent) and toggle completion.
        Task MarkOpenedAsync(Guid lessonId, Guid studentId);
        Task<(bool Success, string Message)> SetCompletedAsync(Guid lessonId, Guid studentId, bool completed);
    }

    public class LessonWithProgress
    {
        public CourseLesson Lesson { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? FirstOpenedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
