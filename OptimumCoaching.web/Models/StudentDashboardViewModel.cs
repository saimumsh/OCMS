using OptimumCoaching.core;

namespace OptimumCoaching.web.Models
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public Student? Student { get; set; }
        public Batch? Batch { get; set; }
        public IList<BatchUpdate> Updates { get; set; } = new List<BatchUpdate>();
    }
}
