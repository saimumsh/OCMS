using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IFeeDueAlertService
    {
        // Returns one row per overdue fee account belonging to the student.
        // "Overdue" = balance > 0 AND today > computed due date.
        Task<IList<DueAlertRow>> GetForStudentAsync(Guid studentId);

        // Admin view — every overdue account across the institution.
        Task<IList<DueAlertRow>> GetAllOverdueAsync();

        // Iterates a student's accounts and, for each newly-crossed due date
        // (or one we haven't alerted since), creates a per-student Notice
        // tagged "fee-overdue". Idempotent: bumps LastDueAlertAt so a second
        // call within the same day does nothing.
        Task<int> EnsureAlertsForStudentAsync(Guid studentId, Guid? actorId);
    }

    public class DueAlertRow
    {
        public Guid AccountId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalOwed => Balance + LateFee;
    }
}
