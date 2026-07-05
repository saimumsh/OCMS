using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class LessonCommentService : ILessonCommentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public LessonCommentService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<LessonComment>> GetForLessonAsync(Guid lessonId) =>
            _db.LessonComments
                .Include(c => c.AuthorUser)
                .Where(c => !c.IsDeleted && c.LessonId == lessonId)
                .OrderBy(c => c.PostedAt)
                .ToListAsync()
                .ContinueWith(t => (IList<LessonComment>)t.Result);

        public async Task<(bool Success, string Message, LessonComment? Comment)> AddAsync(
            Guid lessonId, Guid authorUserId, string body, Guid? parentCommentId)
        {
            if (string.IsNullOrWhiteSpace(body)) return (false, "Comment is required", null);

            // Verify parent exists and belongs to same lesson (when supplied).
            if (parentCommentId.HasValue)
            {
                var parent = await _db.LessonComments.FirstOrDefaultAsync(c =>
                    c.Id == parentCommentId.Value && !c.IsDeleted && c.LessonId == lessonId);
                if (parent == null) return (false, "Parent comment not found", null);
                // Disallow nested replies to keep threading shallow.
                if (parent.ParentCommentId.HasValue) parentCommentId = parent.ParentCommentId;
            }

            var comment = new LessonComment
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                AuthorUserId = authorUserId,
                Body = body.Trim(),
                ParentCommentId = parentCommentId,
                PostedAt = DateTime.UtcNow,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = authorUserId
            };
            _db.LessonComments.Add(comment);
            await _uow.CompleteAsync();
            return (true, "Posted", comment);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid commentId, Guid authorUserId, bool isAdminOverride)
        {
            var c = await _db.LessonComments.FirstOrDefaultAsync(x => x.Id == commentId);
            if (c == null || c.IsDeleted) return (false, "Comment not found");
            if (!isAdminOverride && c.AuthorUserId != authorUserId)
                return (false, "You can only delete your own comments");

            c.IsDeleted = true;
            c.IsActive = false;
            c.LastModified = DateTime.UtcNow;
            c.LastModifiedBy = authorUserId;
            await _uow.CompleteAsync();
            return (true, "Comment removed");
        }
    }

    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public AssignmentService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<Assignment>> GetForBatchAsync(Guid batchId, AssignmentStatus? status = null)
        {
            var q = _db.Assignments
                .Include(a => a.Topic)
                .Where(a => !a.IsDeleted && a.BatchId == batchId);
            if (status.HasValue) q = q.Where(a => a.Status == status.Value);
            return q.OrderByDescending(a => a.DueDate).ToListAsync()
                .ContinueWith(t => (IList<Assignment>)t.Result);
        }

        public Task<Assignment?> GetByIdAsync(Guid id) =>
            _db.Assignments
                .Include(a => a.Batch)
                .Include(a => a.Topic)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        public async Task<(bool Success, string Message, Assignment? Assignment)> CreateAsync(
            Assignment a, Guid? actorId)
        {
            if (string.IsNullOrWhiteSpace(a.Title)) return (false, "Title is required", null);
            if (a.BatchId == Guid.Empty) return (false, "Batch is required", null);
            if (a.MaxScore <= 0) return (false, "Max score must be greater than 0", null);

            a.Id = a.Id == Guid.Empty ? Guid.NewGuid() : a.Id;
            a.IsActive = true;
            a.IsDeleted = false;
            a.Created = DateTime.UtcNow;
            a.CreatedBy = actorId;
            _db.Assignments.Add(a);
            await _uow.CompleteAsync();
            return (true, "Assignment created", a);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(Assignment a, Guid? actorId)
        {
            var existing = await _db.Assignments.FirstOrDefaultAsync(x => x.Id == a.Id);
            if (existing == null || existing.IsDeleted) return (false, "Assignment not found");

            existing.Title = a.Title;
            existing.Instructions = a.Instructions;
            existing.TopicId = a.TopicId;
            existing.DueDate = a.DueDate;
            existing.MaxScore = a.MaxScore;
            existing.AttachmentPath = a.AttachmentPath;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Assignment updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var existing = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Assignment not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Assignment removed");
        }

        public async Task<(bool Success, string Message)> PublishAsync(Guid id, bool publish, Guid? actorId)
        {
            var existing = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Assignment not found");
            existing.Status = publish ? AssignmentStatus.Published : AssignmentStatus.Draft;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, publish ? "Assignment published" : "Assignment unpublished");
        }

        public async Task<IList<Assignment>> GetForStudentAsync(Guid studentId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student?.BatchId == null) return new List<Assignment>();

            return await _db.Assignments
                .Include(a => a.Topic)
                .Where(a => !a.IsDeleted && a.BatchId == student.BatchId
                            && a.Status == AssignmentStatus.Published)
                .OrderBy(a => a.DueDate)
                .ToListAsync();
        }

        public Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentId) =>
            _db.AssignmentSubmissions
                .Include(s => s.GradedByUser)
                .FirstOrDefaultAsync(s => !s.IsDeleted
                                          && s.AssignmentId == assignmentId
                                          && s.StudentId == studentId);

        public Task<IList<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId) =>
            _db.AssignmentSubmissions
                .Include(s => s.Student)
                .Include(s => s.GradedByUser)
                .Where(s => !s.IsDeleted && s.AssignmentId == assignmentId)
                .OrderBy(s => s.Student.FullName)
                .ToListAsync()
                .ContinueWith(t => (IList<AssignmentSubmission>)t.Result);

        public async Task<(bool Success, string Message, AssignmentSubmission? Submission)> SubmitAsync(
            Guid assignmentId, Guid studentId, string? responseText, string? filePath)
        {
            if (string.IsNullOrWhiteSpace(responseText) && string.IsNullOrWhiteSpace(filePath))
                return (false, "Provide a response or upload a file", null);

            var a = await _db.Assignments.FirstOrDefaultAsync(x => x.Id == assignmentId && !x.IsDeleted);
            if (a == null) return (false, "Assignment not found", null);
            if (a.Status != AssignmentStatus.Published) return (false, "Assignment is not open for submission", null);

            var existing = await _db.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
            var now = DateTime.UtcNow;

            if (existing != null)
            {
                if (existing.Status == SubmissionStatus.Graded)
                    return (false, "This submission has already been graded", null);
                existing.ResponseText = responseText;
                if (!string.IsNullOrWhiteSpace(filePath)) existing.FilePath = filePath;
                existing.SubmittedAt = now;
                existing.Status = SubmissionStatus.Submitted;
                existing.LastModified = now;
                existing.LastModifiedBy = studentId;
                await _uow.CompleteAsync();
                return (true, "Resubmitted", existing);
            }

            var s = new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = studentId,
                ResponseText = responseText,
                FilePath = filePath,
                SubmittedAt = now,
                Status = SubmissionStatus.Submitted,
                IsActive = true,
                Created = now,
                CreatedBy = studentId
            };
            _db.AssignmentSubmissions.Add(s);
            await _uow.CompleteAsync();
            return (true, "Submitted", s);
        }

        public async Task<(bool Success, string Message)> GradeAsync(
            Guid submissionId, decimal? score, string? feedback, Guid? graderUserId)
        {
            var s = await _db.AssignmentSubmissions
                .Include(x => x.Assignment)
                .FirstOrDefaultAsync(x => x.Id == submissionId);
            if (s == null || s.IsDeleted) return (false, "Submission not found");
            if (score.HasValue && (score < 0 || score > s.Assignment.MaxScore))
                return (false, $"Score must be between 0 and {s.Assignment.MaxScore}");

            s.Score = score;
            s.Feedback = feedback;
            s.Status = SubmissionStatus.Graded;
            s.GradedByUserId = graderUserId;
            s.GradedAt = DateTime.UtcNow;
            s.LastModified = DateTime.UtcNow;
            s.LastModifiedBy = graderUserId;
            await _uow.CompleteAsync();
            return (true, "Graded");
        }
    }

    public class CourseCatalogService : ICourseCatalogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFeeService _fees;
        private readonly IOnlineEnrollmentService _onlineEnrollments;
        private readonly IUnitOfWork _uow;

        public CourseCatalogService(
            ApplicationDbContext db,
            IFeeService fees,
            IOnlineEnrollmentService onlineEnrollments,
            IUnitOfWork uow)
        {
            _db = db; _fees = fees; _onlineEnrollments = onlineEnrollments; _uow = uow;
        }

        public Task<IList<Batch>> GetPublishedAsync() =>
            _db.Batches
                .Include(b => b.Department)
                .Include(b => b.Subject)
                .Include(b => b.Teacher)
                .Where(b => !b.IsDeleted && b.IsActive && b.IsPublishedForEnrollment)
                .OrderByDescending(b => b.Created)
                .ToListAsync()
                .ContinueWith(t => (IList<Batch>)t.Result);

        public Task<Batch?> GetCourseDetailsAsync(Guid batchId) =>
            _db.Batches
                .Include(b => b.Department)
                .Include(b => b.Subject)
                .Include(b => b.Class)
                .Include(b => b.Teacher)
                .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);

        public async Task<(bool Success, string Message)> EnrollAsync(Guid studentId, Guid batchId, Guid? actorId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null) return (false, "Student profile not found");

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);
            if (batch == null) return (false, "Course not found");
            if (!batch.IsPublishedForEnrollment) return (false, "This course is not open for enrollment");

            // Online + Hybrid batches use the multi-enrollment table — they do
            // NOT touch Student.BatchId, so a student can hold many of them
            // alongside their primary offline batch.
            if (batch.DeliveryMode == DeliveryMode.Online || batch.DeliveryMode == DeliveryMode.Hybrid)
            {
                var (ok, msg, _) = await _onlineEnrollments.EnrollAsync(studentId, batchId, actorId);
                return (ok, msg);
            }

            // ---- Offline path: single-batch reassignment (legacy behavior) ----
            if (student.BatchId == batchId)
            {
                await _fees.EnsureAccountAsync(studentId, batchId, actorId);
                return (true, "You are already enrolled in this course");
            }

            if (batch.Capacity.HasValue)
            {
                var current = await _db.Students.CountAsync(s => !s.IsDeleted && s.BatchId == batchId);
                if (current >= batch.Capacity.Value) return (false, "This course is full");
            }

            student.BatchId = batchId;
            student.LastModified = DateTime.UtcNow;
            student.LastModifiedBy = actorId;
            await _uow.CompleteAsync();

            await _fees.EnsureAccountAsync(studentId, batchId, actorId);
            return (true, "Enrolled — please complete the payment to confirm your seat");
        }
    }
}
