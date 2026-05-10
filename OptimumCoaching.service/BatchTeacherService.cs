using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class BatchTeacherService : IBatchTeacherService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public BatchTeacherService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<BatchTeacher>> GetForBatchAsync(Guid batchId) =>
            _db.BatchTeachers
                .Include(bt => bt.Teacher)
                .Where(bt => !bt.IsDeleted && bt.BatchId == batchId)
                .OrderByDescending(bt => bt.IsLead)
                .ThenBy(bt => bt.Teacher.FullName)
                .ToListAsync()
                .ContinueWith(t => (IList<BatchTeacher>)t.Result);

        public async Task<(bool Success, string Message, BatchTeacher? Assignment)> AddTeacherAsync(
            Guid batchId, Guid teacherId, string? role, string? note, Guid? actorId)
        {
            if (batchId == Guid.Empty || teacherId == Guid.Empty)
                return (false, "Batch and teacher are required", null);

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return (false, "Batch not found", null);

            var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == teacherId && !t.IsDeleted);
            if (!teacherExists) return (false, "Teacher not found", null);

            var existing = await _db.BatchTeachers.FirstOrDefaultAsync(bt =>
                !bt.IsDeleted && bt.BatchId == batchId && bt.TeacherId == teacherId);

            var now = DateTime.UtcNow;
            if (existing != null)
            {
                existing.Role = string.IsNullOrWhiteSpace(role) ? existing.Role : role;
                existing.Note = note;
                existing.LastModified = now;
                existing.LastModifiedBy = actorId;
                await _uow.CompleteAsync();
                return (true, "Assignment updated", existing);
            }

            var assignment = new BatchTeacher
            {
                Id = Guid.NewGuid(),
                BatchId = batchId,
                TeacherId = teacherId,
                Role = string.IsNullOrWhiteSpace(role) ? "Co-teacher" : role,
                IsLead = false,
                AssignedAt = now,
                Note = note,
                IsActive = true,
                Created = now,
                CreatedBy = actorId
            };
            _db.BatchTeachers.Add(assignment);
            await _uow.CompleteAsync();
            return (true, "Teacher added to batch", assignment);
        }

        public async Task<(bool Success, string Message)> RemoveTeacherAsync(
            Guid assignmentId, Guid? actorId)
        {
            var existing = await _db.BatchTeachers
                .Include(bt => bt.Batch)
                .FirstOrDefaultAsync(bt => bt.Id == assignmentId);
            if (existing == null || existing.IsDeleted) return (false, "Assignment not found");

            if (existing.IsLead)
                return (false, "Cannot remove the lead teacher — promote another teacher to lead first");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Teacher removed from batch");
        }

        public async Task<(bool Success, string Message)> SetLeadAsync(
            Guid batchId, Guid teacherId, Guid? actorId)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return (false, "Batch not found");

            var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == teacherId && !t.IsDeleted);
            if (!teacherExists) return (false, "Teacher not found");

            var rows = await _db.BatchTeachers
                .Where(bt => !bt.IsDeleted && bt.BatchId == batchId)
                .ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var r in rows)
            {
                var shouldBeLead = r.TeacherId == teacherId;
                if (r.IsLead == shouldBeLead) continue;
                r.IsLead = shouldBeLead;
                if (shouldBeLead) r.Role = "Lead";
                else if (string.IsNullOrWhiteSpace(r.Role) || r.Role == "Lead") r.Role = "Co-teacher";
                r.LastModified = now;
                r.LastModifiedBy = actorId;
            }

            // Make sure the new lead has a row.
            if (!rows.Any(r => r.TeacherId == teacherId))
            {
                _db.BatchTeachers.Add(new BatchTeacher
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    TeacherId = teacherId,
                    Role = "Lead",
                    IsLead = true,
                    AssignedAt = now,
                    IsActive = true,
                    Created = now,
                    CreatedBy = actorId
                });
            }

            // Sync the denormalized lead reference on Batch itself so existing
            // queries (HomeController, ExamService, etc.) keep working.
            batch.TeacherId = teacherId;
            batch.LastModified = now;
            batch.LastModifiedBy = actorId;

            await _uow.CompleteAsync();
            return (true, "Lead teacher updated");
        }

        public async Task EnsureLeadMirrorAsync(Guid batchId, Guid? newLeadTeacherId, Guid? actorId)
        {
            var rows = await _db.BatchTeachers
                .Where(bt => !bt.IsDeleted && bt.BatchId == batchId)
                .ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var r in rows)
            {
                var shouldBeLead = newLeadTeacherId.HasValue && r.TeacherId == newLeadTeacherId.Value;
                if (r.IsLead != shouldBeLead)
                {
                    r.IsLead = shouldBeLead;
                    if (shouldBeLead) r.Role = "Lead";
                    else if (string.IsNullOrWhiteSpace(r.Role) || r.Role == "Lead") r.Role = "Co-teacher";
                    r.LastModified = now;
                    r.LastModifiedBy = actorId;
                }
            }

            if (newLeadTeacherId.HasValue && !rows.Any(r => r.TeacherId == newLeadTeacherId.Value))
            {
                _db.BatchTeachers.Add(new BatchTeacher
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    TeacherId = newLeadTeacherId.Value,
                    Role = "Lead",
                    IsLead = true,
                    AssignedAt = now,
                    IsActive = true,
                    Created = now,
                    CreatedBy = actorId
                });
            }

            await _uow.CompleteAsync();
        }
    }
}
