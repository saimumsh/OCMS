using OptimumCoaching.core;

namespace OptimumCoaching.web.Models
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
        public Batch? Batch { get; set; }
        public IList<BatchUpdate> Updates { get; set; } = new List<BatchUpdate>();
        public IList<Notice> Notices { get; set; } = new List<Notice>();

        // Teacher-side aggregates.
        public IList<TeacherBatchSummary> TeacherBatches { get; set; } = new List<TeacherBatchSummary>();
        public int TotalStudentsTaught { get; set; }
        public int UpdatesPostedLast30Days { get; set; }
        public int ActiveBatchesCount { get; set; }

        // Student-side: upcoming exams (next 5) and recently published results.
        public IList<Exam> UpcomingExams { get; set; } = new List<Exam>();
        public IList<ExamResult> MyResults { get; set; } = new List<ExamResult>();

        // Student-side classroom data.
        public IList<ClassMaterial> Materials { get; set; } = new List<ClassMaterial>();
        public IList<ClassRoutineSlot> RoutineSlots { get; set; } = new List<ClassRoutineSlot>();

        // Student-side finance.
        public StudentFeeAccount? FeeAccount { get; set; }
        public bool ExamAdmitEligible { get; set; }
        public IList<OptimumCoaching.service.DueAlertRow> DueAlerts { get; set; } = new List<OptimumCoaching.service.DueAlertRow>();

        // Student-side attendance.
        public OptimumCoaching.service.StudentAttendanceSummary? AttendanceSummary { get; set; }

        // Teacher-side: review/rating overview.
        public TeacherRatingSnapshot? RatingSnapshot { get; set; }

        // Featured / advertised courses for the dashboard carousel.
        public IList<Batch> FeaturedCourses { get; set; } = new List<Batch>();

        // Student-side: online (and hybrid) courses the student has signed up
        // for via the public catalog. Separate from `Batch` (their primary
        // offline batch) so a student can hold multiple at once.
        public IList<OptimumCoaching.service.OnlineEnrollmentSummary> MyOnlineCourses { get; set; }
            = new List<OptimumCoaching.service.OnlineEnrollmentSummary>();
    }

    public class TeacherRatingSnapshot
    {
        public double Average { get; set; }
        public int Count { get; set; }
    }

    public class TeacherBatchSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? DepartmentName { get; set; }
        public string? SubjectName { get; set; }
        public string? ClassName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public int RecentUpdates { get; set; } // updates posted in last 30 days

        public bool IsActiveNow =>
            (!StartDate.HasValue || StartDate <= DateTime.UtcNow) &&
            (!EndDate.HasValue   || EndDate   >= DateTime.UtcNow);

        public int FillPercent =>
            Capacity is > 0 ? Math.Min(100, (EnrolledCount * 100) / Capacity!.Value) : 0;
    }
}
