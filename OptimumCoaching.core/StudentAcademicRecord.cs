using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class StudentAcademicRecord : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Required, MaxLength(120), Display(Name = "Examination / Level")]
        public string ExaminationName { get; set; } = string.Empty;

        [Range(1950, 2100), Display(Name = "Passing year")]
        public int PassingYear { get; set; }

        [MaxLength(80), Display(Name = "Group / Department")]
        public string? Group { get; set; }

        [MaxLength(50), Display(Name = "Result (GPA / CGPA / Division)")]
        public string? Result { get; set; }

        [MaxLength(200), Display(Name = "Institution / Board")]
        public string? Institution { get; set; }

        public int SortOrder { get; set; }
    }
}
