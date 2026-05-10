using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IBatchTeacherService
    {
        // Returns every teacher currently attached to the batch, lead first.
        Task<IList<BatchTeacher>> GetForBatchAsync(Guid batchId);

        // Adds a teacher to the batch with an optional Role label and note.
        // Idempotent: re-using an existing (Batch, Teacher) updates the row.
        Task<(bool Success, string Message, BatchTeacher? Assignment)> AddTeacherAsync(
            Guid batchId, Guid teacherId, string? role, string? note, Guid? actorId);

        // Detaches a teacher. Refuses to remove the last lead — promote
        // someone else first.
        Task<(bool Success, string Message)> RemoveTeacherAsync(
            Guid assignmentId, Guid? actorId);

        // Promotes the given teacher to lead (also updates Batch.TeacherId)
        // and demotes the previous lead to a regular co-teacher.
        Task<(bool Success, string Message)> SetLeadAsync(
            Guid batchId, Guid teacherId, Guid? actorId);

        // Mirrors Batch.TeacherId into BatchTeachers as a Lead row. Called by
        // BatchService whenever a batch is created or its TeacherId changes,
        // so the join table stays in sync.
        Task EnsureLeadMirrorAsync(Guid batchId, Guid? newLeadTeacherId, Guid? actorId);
    }
}
