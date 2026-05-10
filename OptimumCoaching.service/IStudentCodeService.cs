using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IStudentCodeService
    {
        // Builds and assigns the StudentCode for a student. Idempotent —
        // returns the existing code unless `force` is true. Requires the
        // student to be approved AND have a Department, Batch, and Session.
        Task<(bool Success, string Message, string? Code)> AssignAsync(
            Guid studentId, bool force = false);

        // Pure helper: derives the next code in the sequence for the given
        // department/batch/session combination, but does not persist anything.
        Task<string> PreviewNextCodeAsync(string deptCode, string session, string batchCode);
    }
}
