using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    // Topics + topic-teacher assignments per batch.
    public interface ITopicService
    {
        Task<IList<Topic>> GetForSubjectAsync(Guid subjectId);
        Task<Topic?> GetByIdAsync(Guid id);
        Task<(bool Success, string Message, Topic? Topic)> CreateAsync(Topic topic, Guid? createdBy);
        Task<(bool Success, string Message)> UpdateAsync(Topic topic, Guid? lastModifiedBy);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy);

        Task<IList<BatchTopicAssignment>> GetAssignmentsForBatchAsync(Guid batchId);
        Task<(bool Success, string Message, BatchTopicAssignment? Assignment)> AssignTeacherAsync(
            Guid batchId, Guid topicId, Guid teacherId, string? note, Guid? actorId);
        Task<(bool Success, string Message)> RemoveAssignmentAsync(Guid assignmentId, Guid? actorId);
    }

    public interface IClassMaterialService
    {
        Task<IList<ClassMaterial>> GetForBatchAsync(Guid batchId);
        Task<ClassMaterial?> GetByIdAsync(Guid id);
        Task<(bool Success, string Message, ClassMaterial? Material)> CreateAsync(ClassMaterial material, Guid? actorId);
        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);
    }

    public interface IClassRoutineService
    {
        Task<IList<ClassRoutineSlot>> GetSlotsForBatchAsync(Guid batchId);
        Task<IList<ClassSessionOverride>> GetOverridesForBatchAsync(Guid batchId, DateTime? from = null, DateTime? to = null);

        Task<(bool Success, string Message, ClassRoutineSlot? Slot)> CreateSlotAsync(ClassRoutineSlot slot, Guid? actorId);
        Task<(bool Success, string Message)> UpdateSlotAsync(ClassRoutineSlot slot, Guid? actorId);
        Task<(bool Success, string Message)> DeleteSlotAsync(Guid slotId, Guid? actorId);

        Task<(bool Success, string Message, ClassSessionOverride? Override)> CreateOverrideAsync(
            ClassSessionOverride sessionOverride, Guid? actorId);
        Task<(bool Success, string Message)> DeleteOverrideAsync(Guid overrideId, Guid? actorId);
    }
}
