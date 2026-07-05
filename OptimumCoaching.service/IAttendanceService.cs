using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IAttendanceService
    {
        // Loads (or creates) the session for the given (Batch, Date) and
        // returns one row per enrolled student — hydrated from existing
        // AttendanceRecord rows when present.
        Task<AttendanceMarkingGrid> BuildMarkingGridAsync(Guid batchId, DateTime date);

        Task<(bool Success, string Message, AttendanceSession? Session)> SaveAsync(
            Guid batchId, DateTime date, Guid? topicId, string? note,
            IList<AttendanceRowInput> rows, Guid? actorId);

        Task<IList<AttendanceSession>> GetSessionsForBatchAsync(
            Guid batchId, DateTime? from = null, DateTime? to = null);

        Task<AttendanceSession?> GetSessionAsync(Guid sessionId);

        // Aggregate stats: total sessions, present count, present-percentage
        // for a given student (optionally scoped to a batch).
        Task<StudentAttendanceSummary> GetStudentSummaryAsync(Guid studentId, Guid? batchId = null);
    }

    public class AttendanceMarkingGrid
    {
        public Guid BatchId { get; set; }
        public DateTime SessionDate { get; set; }
        public Guid? SessionId { get; set; } // null = not yet saved
        public Guid? TopicId { get; set; }
        public string? Note { get; set; }
        public IList<AttendanceRow> Rows { get; set; } = new List<AttendanceRow>();
    }

    public class AttendanceRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.NotMarked;
        public string? Remarks { get; set; }
    }

    public class AttendanceRowInput
    {
        public Guid StudentId { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class StudentAttendanceSummary
    {
        public int TotalSessions { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public int ExcusedCount { get; set; }

        // Late counts as present for percentage purposes; Excused excluded.
        public int Eligible => TotalSessions - ExcusedCount;
        public int Attended => PresentCount + LateCount;
        public int PercentPresent => Eligible > 0 ? (int)Math.Round(Attended * 100.0 / Eligible) : 0;
    }
}
