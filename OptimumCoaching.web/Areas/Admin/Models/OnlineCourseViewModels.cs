using OptimumCoaching.core;

namespace OptimumCoaching.web.Areas.Admin.Models
{
    public class OnlineCourseListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public DeliveryMode DeliveryMode { get; set; }
        public decimal CourseFee { get; set; }
        public decimal? OfferedPrice { get; set; }
        public DateTime? OfferEndsAt { get; set; }
        public string? OfferLabel { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? PromoVideoUrl { get; set; }
        public bool IsPublishedForEnrollment { get; set; }
        public bool IsActive { get; set; }
        public int? Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public int LessonsCount { get; set; }
    }

    public class OnlineCourseDetailsViewModel
    {
        public Batch Batch { get; set; } = null!;
        public IList<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public int LessonsCount { get; set; }
        public decimal RevenuePaid { get; set; }
        public decimal RevenueOutstanding { get; set; }
    }
}
