using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Per-Student-per-Batch fee ledger header. Holds the final amount owed
    // (after any discounts) and the running balance. One row exists per
    // (Student, Batch) pair and is created automatically when a student is
    // assigned to a batch.
    public class StudentFeeAccount : AuditableEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        // The fee the student actually owes after discounts/overrides.
        // Defaults to Batch.CourseFee on creation.
        [Display(Name = "Final fee")]
        public decimal FinalFee { get; set; }

        // Sum of FeePayment.Amount values (denormalized for fast read).
        [Display(Name = "Paid")]
        public decimal AmountPaid { get; set; }

        // Discount actually applied (currency, not %), for audit/display.
        [Display(Name = "Discount")]
        public decimal DiscountAmount { get; set; }

        [MaxLength(500), Display(Name = "Discount reason")]
        public string? DiscountReason { get; set; }

        public FeeAccountStatus Status { get; set; } = FeeAccountStatus.Unpaid;

        public DateTime? FullyPaidOn { get; set; }

        public decimal Balance => FinalFee - AmountPaid;

        public IList<FeePayment> Payments { get; set; } = new List<FeePayment>();
    }

    public class FeePayment : AuditableEntity
    {
        public Guid AccountId { get; set; }
        public StudentFeeAccount Account { get; set; } = null!;

        [Range(0.01, double.MaxValue), Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Paid on")]
        public DateTime PaidOn { get; set; } = DateTime.UtcNow;

        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        [MaxLength(50), Display(Name = "Receipt #")]
        public string? ReceiptNumber { get; set; }

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }

        public Guid? RecordedByUserId { get; set; }
        public ApplicationUser? RecordedByUser { get; set; }
    }

    // Simple monthly salary ledger row per Teacher.
    public class TeacherSalaryPayment : AuditableEntity
    {
        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        // The pay period (year & month). We store the first day of the month.
        [Display(Name = "Pay period")]
        public DateTime PeriodMonth { get; set; }

        [Range(0.01, double.MaxValue), Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Paid on")]
        public DateTime PaidOn { get; set; } = DateTime.UtcNow;

        public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

        public SalaryPaymentStatus Status { get; set; } = SalaryPaymentStatus.Paid;

        [MaxLength(50), Display(Name = "Reference #")]
        public string? Reference { get; set; }

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }

        public Guid? RecordedByUserId { get; set; }
        public ApplicationUser? RecordedByUser { get; set; }
    }
}
