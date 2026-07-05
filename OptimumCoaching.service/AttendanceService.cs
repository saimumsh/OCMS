using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;
using OptimumCoaching.repo.Core;

namespace OptimumCoaching.service
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public AttendanceService(ApplicationDbContext db, IUnitOfWork uow)
        {
            _db = db; _uow = uow;
        }

        public async Task<AttendanceMarkingGrid> BuildMarkingGridAsync(Guid batchId, DateTime date)
        {
            var day = date.Date;
            var session = await _db.AttendanceSessions
                .Include(s => s.Records)
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.BatchId == batchId && s.SessionDate == day);

            var students = await _db.Students
                .Where(s => !s.IsDeleted && s.BatchId == batchId)
                .OrderBy(s => s.FullName)
                .Select(s => new { s.Id, s.FullName, s.StudentCode })
                .ToListAsync();

            var existingByStudent = session?.Records
                .Where(r => !r.IsDeleted)
                .ToDictionary(r => r.StudentId)
                ?? new Dictionary<Guid, AttendanceRecord>();

            return new AttendanceMarkingGrid
            {
                BatchId = batchId,
                SessionDate = day,
                SessionId = session?.Id,
                TopicId = session?.TopicId,
                Note = session?.Note,
                Rows = students.Select(s =>
                {
                    existingByStudent.TryGetValue(s.Id, out var r);
                    return new AttendanceRow
                    {
                        StudentId = s.Id,
                        StudentName = s.FullName,
                        StudentCode = s.StudentCode,
                        Status = r?.Status ?? AttendanceStatus.Present, // default Present to speed marking
                        Remarks = r?.Remarks
                    };
                }).ToList()
            };
        }

        public async Task<(bool Success, string Message, AttendanceSession? Session)> SaveAsync(
            Guid batchId, DateTime date, Guid? topicId, string? note,
            IList<AttendanceRowInput> rows, Guid? actorId)
        {
            if (batchId == Guid.Empty) return (false, "Batch is required", null);
            if (rows == null || rows.Count == 0) return (false, "No students to save", null);

            var day = date.Date;
            var now = DateTime.UtcNow;

            var session = await _db.AttendanceSessions
                .Include(s => s.Records)
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.BatchId == batchId && s.SessionDate == day);

            if (session == null)
            {
                session = new AttendanceSession
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    SessionDate = day,
                    TopicId = topicId,
                    Note = note,
                    TakenByUserId = actorId,
                    IsActive = true,
                    Created = now,
                    CreatedBy = actorId
                };
                _db.AttendanceSessions.Add(session);
            }
            else
            {
                session.TopicId = topicId;
                session.Note = note;
                session.TakenByUserId = actorId;
                session.LastModified = now;
                session.LastModifiedBy = actorId;
            }

            // Upsert each student row.
            var existing = session.Records.Where(r => !r.IsDeleted).ToDictionary(r => r.StudentId);
            foreach (var input in rows)
            {
                if (input.StudentId == Guid.Empty) continue;
                if (existing.TryGetValue(input.StudentId, out var er))
                {
                    er.Status = input.Status;
                    er.Remarks = input.Remarks;
                    er.LastModified = now;
                    er.LastModifiedBy = actorId;
                }
                else
                {
                    _db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        StudentId = input.StudentId,
                        Status = input.Status,
                        Remarks = input.Remarks,
                        IsActive = true,
                        Created = now,
                        CreatedBy = actorId
                    });
                }
            }

            await _uow.CompleteAsync();
            return (true, $"Attendance saved for {day:dd MMM yyyy}", session);
        }

        public Task<IList<AttendanceSession>> GetSessionsForBatchAsync(
            Guid batchId, DateTime? from = null, DateTime? to = null)
        {
            var q = _db.AttendanceSessions
                .Include(s => s.Topic)
                .Include(s => s.TakenByUser)
                .Include(s => s.Records.Where(r => !r.IsDeleted)) // needed for P/L/A/E counts in the Index view
                .Where(s => !s.IsDeleted && s.BatchId == batchId);
            if (from.HasValue) q = q.Where(s => s.SessionDate >= from.Value.Date);
            if (to.HasValue)   q = q.Where(s => s.SessionDate <= to.Value.Date);
            return q.OrderByDescending(s => s.SessionDate).ToListAsync()
                .ContinueWith(t => (IList<AttendanceSession>)t.Result);
        }

        public Task<AttendanceSession?> GetSessionAsync(Guid sessionId) =>
            _db.AttendanceSessions
                .Include(s => s.Batch)
                .Include(s => s.Topic)
                .Include(s => s.TakenByUser)
                .Include(s => s.Records.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

        public async Task<StudentAttendanceSummary> GetStudentSummaryAsync(Guid studentId, Guid? batchId = null)
        {
            var q = _db.AttendanceRecords
                .Where(r => !r.IsDeleted && r.StudentId == studentId);
            if (batchId.HasValue)
                q = q.Where(r => r.Session.BatchId == batchId.Value);

            // One pass on the server, group + count by status.
            var grouped = await q
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int Get(AttendanceStatus s) => grouped.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

            var summary = new StudentAttendanceSummary
            {
                PresentCount = Get(AttendanceStatus.Present),
                LateCount = Get(AttendanceStatus.Late),
                AbsentCount = Get(AttendanceStatus.Absent),
                ExcusedCount = Get(AttendanceStatus.Excused)
            };
            summary.TotalSessions = summary.PresentCount + summary.LateCount + summary.AbsentCount + summary.ExcusedCount;
            return summary;
        }
    }
}
