using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IExamService
    {
        Task<IList<Exam>> GetAllAsync(Guid? batchId = null, ExamStatus? status = null);
        Task<IList<Exam>> GetUpcomingForStudentAsync(Guid studentId, int take = 5);
        Task<IList<Exam>> GetForTeacherAsync(Guid teacherId, ExamStatus? status = null);
        Task<Exam?> GetByIdAsync(Guid id);

        Task<(bool Success, string Message, Exam? Exam)> CreateAsync(Exam exam, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Exam exam, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> PublishAsync(Guid id, Guid? actorId);
    }

    public interface IExamResultService
    {
        // Returns one row per enrolled student in the exam's batch, hydrating
        // existing ExamResult rows when present, or providing empty placeholders
        // when not yet entered.
        Task<IList<ExamResultRow>> BuildGradingGridAsync(Guid examId);

        Task<(bool Success, string Message)> SaveDraftAsync(
            Guid examId, IList<ExamResultRow> rows, Guid? actorId);

        // Marks ALL result rows for the exam as Published (and stamps PublishedAt
        // / PublishedByUserId on each).
        Task<(bool Success, string Message)> PublishAllAsync(Guid examId, Guid? actorId);

        Task<IList<ExamResult>> GetForStudentAsync(Guid studentId, bool publishedOnly = true);
        Task<IList<ExamResult>> GetForExamAsync(Guid examId);
    }

    public class ExamResultRow
    {
        public Guid? ResultId { get; set; }   // null when no row exists yet
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public bool IsPresent { get; set; } = true;
        public decimal? MarksObtained { get; set; }
        public string? Remarks { get; set; }
        public ResultStatus Status { get; set; } = ResultStatus.Draft;
    }
}
