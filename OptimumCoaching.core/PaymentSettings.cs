using OptimumCoaching.core.Core;
using System.ComponentModel.DataAnnotations;

namespace OptimumCoaching.core
{
    // Global, single-row config for the payments module. The application
    // ensures exactly one row exists (auto-created by the seeder if missing).
    public class PaymentSettings : AuditableEntity
    {
        [MaxLength(20), Display(Name = "Currency symbol")]
        public string CurrencySymbol { get; set; } = "৳";

        [MaxLength(40), Display(Name = "Receipt prefix")]
        public string ReceiptPrefix { get; set; } = "OCMS-";

        [Display(Name = "Next receipt #")]
        public int NextReceiptNumber { get; set; } = 1;

        // CSV of `PaymentMethod` int values that admins want exposed in the
        // recording UI. Empty = all methods enabled.
        [MaxLength(200), Display(Name = "Enabled methods")]
        public string? EnabledMethodsCsv { get; set; }
    }

    // Discount tier configurable in admin settings. When a fee account is
    // created (or a student requests application), the system finds the
    // best-fit tier based on the student's most recent published result %.
    public class ResultDiscountTier : AuditableEntity
    {
        [Required, MaxLength(80), Display(Name = "Tier name")]
        public string Name { get; set; } = string.Empty;

        // The minimum result percentage a student must score to qualify.
        [Display(Name = "Minimum result %")]
        public decimal MinResultPercent { get; set; }

        // Discount percentage off the course fee (0–100).
        [Display(Name = "Discount %")]
        public decimal DiscountPercent { get; set; }
    }
}
