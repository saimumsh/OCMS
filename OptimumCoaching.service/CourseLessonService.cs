using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class CourseLessonService : ICourseLessonService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public CourseLessonService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public Task<IList<CourseLesson>> GetForBatchAsync(Guid batchId, bool publishedOnly = false)
        {
            var q = _db.CourseLessons
                .Include(l => l.Topic)
                .Where(l => !l.IsDeleted && l.BatchId == batchId);
            if (publishedOnly) q = q.Where(l => l.IsPublished);
            return q.OrderBy(l => l.SortOrder).ThenBy(l => l.Title)
                .ToListAsync()
                .ContinueWith(t => (IList<CourseLesson>)t.Result);
        }

        public Task<CourseLesson?> GetByIdAsync(Guid id) =>
            _db.CourseLessons
                .Include(l => l.Topic)
                .Include(l => l.Batch)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        public async Task<IList<LessonWithProgress>> GetForStudentAsync(Guid studentId, Guid batchId)
        {
            var lessons = await GetForBatchAsync(batchId, publishedOnly: true);
            var lessonIds = lessons.Select(l => l.Id).ToList();
            var progress = await _db.StudentLessonProgresses
                .Where(p => !p.IsDeleted && p.StudentId == studentId && lessonIds.Contains(p.LessonId))
                .ToDictionaryAsync(p => p.LessonId);

            return lessons.Select(l =>
            {
                progress.TryGetValue(l.Id, out var p);
                return new LessonWithProgress
                {
                    Lesson = l,
                    IsCompleted = p?.IsCompleted ?? false,
                    FirstOpenedAt = p?.FirstOpenedAt,
                    CompletedAt = p?.CompletedAt
                };
            }).ToList();
        }

        public async Task<(bool Success, string Message, CourseLesson? Lesson)> CreateAsync(
            CourseLesson lesson, Guid? actorId)
        {
            if (string.IsNullOrWhiteSpace(lesson.Title))
                return (false, "Title is required", null);
            if (lesson.BatchId == Guid.Empty)
                return (false, "Batch is required", null);
            if (string.IsNullOrWhiteSpace(lesson.VideoUrl) && string.IsNullOrWhiteSpace(lesson.FilePath))
                return (false, "Provide either a video URL or upload a file", null);

            lesson.Id = lesson.Id == Guid.Empty ? Guid.NewGuid() : lesson.Id;
            lesson.IsActive = true;
            lesson.IsDeleted = false;
            lesson.Created = DateTime.UtcNow;
            lesson.CreatedBy = actorId;
            _db.CourseLessons.Add(lesson);
            await _uow.CompleteAsync();
            return (true, "Lesson added", lesson);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(CourseLesson lesson, Guid? actorId)
        {
            var existing = await _db.CourseLessons.FirstOrDefaultAsync(l => l.Id == lesson.Id);
            if (existing == null || existing.IsDeleted) return (false, "Lesson not found");

            existing.Title = lesson.Title;
            existing.Description = lesson.Description;
            existing.SortOrder = lesson.SortOrder;
            existing.TopicId = lesson.TopicId;
            existing.VideoUrl = lesson.VideoUrl;
            existing.FilePath = lesson.FilePath;
            existing.ResourcePath = lesson.ResourcePath;
            existing.DurationMinutes = lesson.DurationMinutes;
            existing.IsPublished = lesson.IsPublished;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Lesson updated");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId)
        {
            var existing = await _db.CourseLessons.FirstOrDefaultAsync(l => l.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Lesson not found");
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, "Lesson removed");
        }

        public async Task<(bool Success, string Message)> PublishAsync(Guid id, bool publish, Guid? actorId)
        {
            var existing = await _db.CourseLessons.FirstOrDefaultAsync(l => l.Id == id);
            if (existing == null || existing.IsDeleted) return (false, "Lesson not found");
            existing.IsPublished = publish;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = actorId;
            await _uow.CompleteAsync();
            return (true, publish ? "Lesson published" : "Lesson unpublished");
        }

        public async Task MarkOpenedAsync(Guid lessonId, Guid studentId)
        {
            var existing = await _db.StudentLessonProgresses
                .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.StudentId == studentId);
            if (existing != null)
            {
                existing.FirstOpenedAt ??= DateTime.UtcNow;
            }
            else
            {
                _db.StudentLessonProgresses.Add(new StudentLessonProgress
                {
                    LessonId = lessonId,
                    StudentId = studentId,
                    FirstOpenedAt = DateTime.UtcNow,
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    CreatedBy = studentId
                });
            }
            await _uow.CompleteAsync();
        }

        public async Task<(bool Success, string Message)> SetCompletedAsync(Guid lessonId, Guid studentId, bool completed)
        {
            var existing = await _db.StudentLessonProgresses
                .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.StudentId == studentId);
            var now = DateTime.UtcNow;

            if (existing == null)
            {
                _db.StudentLessonProgresses.Add(new StudentLessonProgress
                {
                    LessonId = lessonId,
                    StudentId = studentId,
                    FirstOpenedAt = now,
                    CompletedAt = completed ? now : null,
                    IsActive = true,
                    Created = now,
                    CreatedBy = studentId
                });
            }
            else
            {
                existing.CompletedAt = completed ? now : null;
                existing.LastModified = now;
                existing.LastModifiedBy = studentId;
            }
            await _uow.CompleteAsync();
            return (true, completed ? "Marked completed" : "Marked incomplete");
        }
    }
}
