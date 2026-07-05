using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    // ---- Lesson comments (per-lesson discussion thread) ----
    public interface ILessonCommentService
    {
        Task<IList<LessonComment>> GetForLessonAsync(Guid lessonId);
        Task<(bool Success, string Message, LessonComment? Comment)> AddAsync(
            Guid lessonId, Guid authorUserId, string body, Guid? parentCommentId);
        Task<(bool Success, string Message)> DeleteAsync(Guid commentId, Guid authorUserId, bool isAdminOverride);
    }

    // ---- Assignments ----
    public interface IAssignmentService
    {
        Task<IList<Assignment>> GetForBatchAsync(Guid batchId, AssignmentStatus? status = null);
        Task<Assignment?> GetByIdAsync(Guid id);
        Task<(bool Success, string Message, Assignment? Assignment)> CreateAsync(Assignment assignment, Guid? actorId);
        Task<(bool Success, string Message)> UpdateAsync(Assignment assignment, Guid? actorId);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);
        Task<(bool Success, string Message)> PublishAsync(Guid id, bool publish, Guid? actorId);

        Task<IList<Assignment>> GetForStudentAsync(Guid studentId);
        Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentId);
        Task<IList<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId);

        Task<(bool Success, string Message, AssignmentSubmission? Submission)> SubmitAsync(
            Guid assignmentId, Guid studentId, string? responseText, string? filePath);

        Task<(bool Success, string Message)> GradeAsync(
            Guid submissionId, decimal? score, string? feedback, Guid? graderUserId);
    }

    // ---- Public course catalog + enrollment ----
    public interface ICourseCatalogService
    {
        Task<IList<Batch>> GetPublishedAsync();
        Task<Batch?> GetCourseDetailsAsync(Guid batchId);

        // Enrolls the given student in the batch and creates their fee account.
        // Idempotent: if already enrolled, returns Success with a clear message.
        Task<(bool Success, string Message)> EnrollAsync(Guid studentId, Guid batchId, Guid? actorId);
    }
}
