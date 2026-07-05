using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public class Batch : AuditableEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }

        [Display(Name = "Subject")]
        public Guid? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Display(Name = "Teacher")]
        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Capacity")]
        public int? Capacity { get; set; }

        // ---- Fees ---------------------------------------------------
        // Headline course fee for this batch. The actual amount each student
        // owes is stored on StudentFeeAccount (after any discounts).
        [Display(Name = "Course fee")]
        public decimal CourseFee { get; set; }

        // Minimum a student must pay to be considered "enrolled" (controls
        // exam-admit-card eligibility, etc.).
        [Display(Name = "Minimum to enroll")]
        public decimal MinimumEnrollment { get; set; }

        // Discount % applied automatically when a student clears the entire
        // course fee in a single payment (0–100).
        [Display(Name = "Full-payment discount %")]
        public decimal FullPaymentDiscountPercent { get; set; }

        // ---- Late-fee policy ----------------------------------------
        // Absolute due date for the full course fee. If null, due-days
        // (counted from enrollment) is used instead.
        [Display(Name = "Fee due date")]
        public DateTime? FeeDueDate { get; set; }

        // Days from a student's EnrollmentDate after which the fee is
        // considered overdue. Ignored when FeeDueDate is set.
        [Display(Name = "Due days after enrollment")]
        public int? FeeDueDays { get; set; }

        // One-time penalty added the moment the fee becomes overdue.
        [Display(Name = "Late fee (flat)")]
        public decimal LateFeeFlat { get; set; }

        // Per-day penalty accrued from the due date onward.
        [Display(Name = "Late fee per day")]
        public decimal LateFeePerDay { get; set; }

        // ---- Delivery mode ----------------------------------------
        // Whether sessions are held in person, online, or both.
        [Display(Name = "Delivery mode")]
        public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.Offline;

        // Default live-class meeting URL (Zoom / Meet / etc.) for the batch.
        // Individual ClassRoutineSlot / ClassSessionOverride rows may override.
        [MaxLength(500), Url, Display(Name = "Meeting link")]
        public string? MeetingUrl { get; set; }

        [MaxLength(1000), Display(Name = "Meeting notes")]
        public string? MeetingNotes { get; set; }

        // ---- Public catalog ----------------------------------------
        // When true the batch shows up in the public course catalog and
        // signed-in students can enroll themselves.
        [Display(Name = "Open for enrollment")]
        public bool IsPublishedForEnrollment { get; set; }

        [MaxLength(500), Display(Name = "Short description")]
        public string? ShortDescription { get; set; }

        [MaxLength(500), Display(Name = "Cover image URL")]
        public string? CoverImageUrl { get; set; }

        // Promotional/intro video (YouTube/Vimeo/Drive URL) shown on the
        // course details page. Optional.
        [MaxLength(500), Url, Display(Name = "Promo video URL")]
        public string? PromoVideoUrl { get; set; }

        // ---- Limited-time offers ----------------------------------
        // When set, the catalog shows an "offer" ribbon with this label and
        // the price is displayed as OfferedPrice (with CourseFee struck out).
        [MaxLength(80), Display(Name = "Offer label")]
        public string? OfferLabel { get; set; }

        [Display(Name = "Offer price")]
        public decimal? OfferedPrice { get; set; }

        [Display(Name = "Offer ends on")]
        public DateTime? OfferEndsAt { get; set; }
    }
}
