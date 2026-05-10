using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class TopicService : ITopicService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public TopicService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<Topic>> GetForSubjectAsync(Guid subjectId) =>
            _db.Topics
                .Where(t => !t.IsDeleted && t.SubjectId == subjectId)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Title)
                .ToListAsync()
                .ContinueWith(t => (IList<Topic>)t.Result);

        public Task<Topic?> GetByIdAsync(Guid id) =>
            _db.Topics.Include(t => t.Subject).FirstOrDefaultAsync(t => t.Id == id);

        public async Task<(bool Success, string Message, Topic? Topic)> CreateAsync(Topic topic, Guid? createdBy)
        {
            if (string.IsNullOrWhiteSpace(topic.Title))
                return (false, "Title is required", null);
            if (topic.SubjectId == Guid.Empty)
                return (false, "Subject is required", null);

            var dup = await _db.Topics.AnyAsync(t =>
                !t.IsDeleted && t.SubjectId == topic.SubjectId && t.Title == topic.Title);
            if (dup) return (false, "A topic with this title already exists in the subject", null);

            topic.Id = topic.Id == Guid.Empty ? Guid.NewGuid() : topic.Id;
            topic.IsActive = true;
            topic.IsDeleted = false;
            topic.Created = DateTime.UtcNow;
            topic.CreatedBy = createdBy;
            _db.Topics.Add(topic);
            await _uow.CompleteAsync();
            return (true, "Topic created", topic);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Topic topic, Guid? lastModifiedBy)
        {
            var existing = await _db.Topics.FirstOrDefaultAsync(t => t.Id == topic.Id);
            if (existing == null || existing.IsDeleted) return (false, "Topic not found");

            existing.Title = topic.Title;
            existing.Code = topic.Code;
            existing.Description = topic.Description;
            existing.SortOrder = topic.SortOrder;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;
            await _uow.CompleteAsync();
            return (true, "Topic updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? lastModifiedBy)
        {
            var existing = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Topic not found");

            var inUse = await _db.BatchTopicAssignments.AnyAsync(a => a.TopicId == id && !a.IsDeleted);
            if (inUse) return (false, "Cannot delete — this topic is assigned to one or more batches");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = lastModifiedBy;
            await _uow.CompleteAsync();
            return (true, "Topic removed");
        }

        public Task<IList<BatchTopicAssignment>> GetAssignmentsForBatchAsync(Guid batchId) =>
            _db.BatchTopicAssignments
                .Include(a => a.Topic)
                .Include(a => a.Teacher)
                .Where(a => !a.IsDeleted && a.BatchId == batchId)
                .OrderBy(a => a.Topic.SortOrder).ThenBy(a => a.Topic.Title)
                .ToListAsync()
                .ContinueWith(t => (IList<BatchTopicAssignment>)t.Result);

        public async Task<(bool Success, string Message, BatchTopicAssignment? Assignment)> AssignTeacherAsync(
            Guid batchId, Guid topicId, Guid teacherId, string? note, Guid? actorId)
        {
            var existing = await _db.BatchTopicAssignments
                .FirstOrDefaultAsync(a => !a.IsDeleted && a.BatchId == batchId && a.TopicId == topicId);

            if (existing != null)
            {
                existing.TeacherId = teacherId;
                existing.Note = note;
                existing.LastModified = DateTime.UtcNow;
                existing.LastModifiedBy = actorId;
                await _uow.CompleteAsync();
                return (true, "Teacher reassigned", existing);
            }

            var assignment = new BatchTopicAssignment
            {
                Id = Guid.NewGuid(),
                BatchId = batchId,
                TopicId = topicId,
                TeacherId = teacherId,
                Note = note,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = actorId
            };
            _db.BatchTopicAssignments.Add(assignment);
            await _uow.CompleteAsync();
            return (true, "Teacher assigned to topic", assignment);
        }

        public async Task<(bool Success, string Message)> RemoveAssignmentAsync(Guid assignmentId, Guid? actorId)
        {
            var existing = await _db.BatchTopicAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
            if (existing == null || existing.IsDeleted) return (false, "Assignment not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Assignment removed");
        }
    }

    public class ClassMaterialService : IClassMaterialService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public ClassMaterialService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<ClassMaterial>> GetForBatchAsync(Guid batchId) =>
            _db.ClassMaterials
                .Include(m => m.Topic)
                .Include(m => m.UploadedByUser)
                .Where(m => !m.IsDeleted && m.BatchId == batchId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync()
                .ContinueWith(t => (IList<ClassMaterial>)t.Result);

        public Task<ClassMaterial?> GetByIdAsync(Guid id) =>
            _db.ClassMaterials
                .Include(m => m.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<(bool Success, string Message, ClassMaterial? Material)> CreateAsync(
            ClassMaterial material, Guid? actorId)
        {
            if (string.IsNullOrWhiteSpace(material.Title))
                return (false, "Title is required", null);
            if (material.BatchId == Guid.Empty)
                return (false, "Batch is required", null);
            if (string.IsNullOrWhiteSpace(material.Url) && string.IsNullOrWhiteSpace(material.FilePath))
                return (false, "Provide either a URL or upload a file", null);

            material.Id = material.Id == Guid.Empty ? Guid.NewGuid() : material.Id;
            material.UploadedByUserId = actorId;
            material.UploadedAt = DateTime.UtcNow;
            material.IsActive = true;
            material.IsDeleted = false;
            material.Created = DateTime.UtcNow;
            material.CreatedBy = actorId;
            _db.ClassMaterials.Add(material);
            await _uow.CompleteAsync();
            return (true, "Material added", material);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var existing = await _db.ClassMaterials.FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Material not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Material removed");
        }
    }

    public class ClassRoutineService : IClassRoutineService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public ClassRoutineService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<ClassRoutineSlot>> GetSlotsForBatchAsync(Guid batchId) =>
            _db.ClassRoutineSlots
                .Include(s => s.Topic)
                .Include(s => s.Teacher)
                .Where(s => !s.IsDeleted && s.BatchId == batchId)
                .OrderBy(s => s.Day).ThenBy(s => s.StartTime)
                .ToListAsync()
                .ContinueWith(t => (IList<ClassRoutineSlot>)t.Result);

        public Task<IList<ClassSessionOverride>> GetOverridesForBatchAsync(
            Guid batchId, DateTime? from = null, DateTime? to = null)
        {
            var q = _db.ClassSessionOverrides
                .Include(o => o.Topic)
                .Include(o => o.Teacher)
                .Include(o => o.RoutineSlot)
                .Where(o => !o.IsDeleted && o.BatchId == batchId);
            if (from.HasValue) q = q.Where(o => o.SessionDate >= from.Value);
            if (to.HasValue)   q = q.Where(o => o.SessionDate <= to.Value);
            return q.OrderBy(o => o.SessionDate).ToListAsync()
                .ContinueWith(t => (IList<ClassSessionOverride>)t.Result);
        }

        public async Task<(bool Success, string Message, ClassRoutineSlot? Slot)> CreateSlotAsync(
            ClassRoutineSlot slot, Guid? actorId)
        {
            if (slot.BatchId == Guid.Empty)
                return (false, "Batch is required", null);
            if (slot.EndTime <= slot.StartTime)
                return (false, "End time must be after start time", null);

            slot.Id = slot.Id == Guid.Empty ? Guid.NewGuid() : slot.Id;
            slot.IsActive = true;
            slot.IsDeleted = false;
            slot.Created = DateTime.UtcNow;
            slot.CreatedBy = actorId;
            _db.ClassRoutineSlots.Add(slot);
            await _uow.CompleteAsync();
            return (true, "Slot added", slot);
        }

        public async Task<(bool Success, string Message)> UpdateSlotAsync(ClassRoutineSlot slot, Guid? actorId)
        {
            var existing = await _db.ClassRoutineSlots.FirstOrDefaultAsync(s => s.Id == slot.Id);
            if (existing == null || existing.IsDeleted) return (false, "Slot not found");
            if (slot.EndTime <= slot.StartTime) return (false, "End time must be after start time");

            existing.Day = slot.Day;
            existing.StartTime = slot.StartTime;
            existing.EndTime = slot.EndTime;
            existing.Room = slot.Room;
            existing.TopicId = slot.TopicId;
            existing.TeacherId = slot.TeacherId;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Slot updated");
        }

        public async Task<(bool Success, string Message)> DeleteSlotAsync(Guid slotId, Guid? actorId)
        {
            var existing = await _db.ClassRoutineSlots.FirstOrDefaultAsync(s => s.Id == slotId);
            if (existing == null || existing.IsDeleted) return (false, "Slot not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Slot removed");
        }

        public async Task<(bool Success, string Message, ClassSessionOverride? Override)> CreateOverrideAsync(
            ClassSessionOverride sessionOverride, Guid? actorId)
        {
            if (sessionOverride.BatchId == Guid.Empty)
                return (false, "Batch is required", null);
            if (sessionOverride.StartTime.HasValue && sessionOverride.EndTime.HasValue
                && sessionOverride.EndTime <= sessionOverride.StartTime)
                return (false, "End time must be after start time", null);

            sessionOverride.Id = sessionOverride.Id == Guid.Empty ? Guid.NewGuid() : sessionOverride.Id;
            sessionOverride.IsActive = true;
            sessionOverride.IsDeleted = false;
            sessionOverride.Created = DateTime.UtcNow;
            sessionOverride.CreatedBy = actorId;
            _db.ClassSessionOverrides.Add(sessionOverride);
            await _uow.CompleteAsync();
            return (true, sessionOverride.IsCancelled ? "Session cancelled" : "Session override saved", sessionOverride);
        }

        public async Task<(bool Success, string Message)> DeleteOverrideAsync(Guid overrideId, Guid? actorId)
        {
            var existing = await _db.ClassSessionOverrides.FirstOrDefaultAsync(o => o.Id == overrideId);
            if (existing == null || existing.IsDeleted) return (false, "Override not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Override removed");
        }
    }
}
