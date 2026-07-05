using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    public enum FeePaymentRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    // A self-service payment submission a Student files from their dashboard.
    // Finance/Admin verifies it and either approves (creating a real FeePayment
    // on the StudentFeeAccount) or rejects with a reason. The submitted receipt
    // image is stored under /uploads/payment-receipts.
    public class FeePaymentRequest : AuditableEntity
    {
        public Guid AccountId { get; set; }
        public StudentFeeAccount Account { get; set; } = null!;

        // The student's login account that filed the request.
        public Guid SubmittedByUserId { get; set; }
        public ApplicationUser? SubmittedByUser { get; set; }

        [Range(0.01, double.MaxValue), Display(Name = "Amount")]
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.bKash;

        [MaxLength(100), Display(Name = "Transaction reference")]
        public string? TransactionReference { get; set; }

        // Relative path under wwwroot to the uploaded receipt screenshot.
        [MaxLength(500), Display(Name = "Receipt image")]
        public string? ReceiptImagePath { get; set; }

        [MaxLength(500), Display(Name = "Note")]
        public string? Note { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public FeePaymentRequestStatus Status { get; set; } = FeePaymentRequestStatus.Pending;

        public Guid? ReviewedByUserId { get; set; }
        public ApplicationUser? ReviewedByUser { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500), Display(Name = "Reason for rejection")]
        public string? RejectionReason { get; set; }

        // Set when approved → the matching FeePayment row that landed on the account.
        public Guid? LinkedPaymentId { get; set; }
    }
}
