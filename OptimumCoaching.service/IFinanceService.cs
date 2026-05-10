using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IFeeService
    {
        // Creates the per-student fee account (idempotent). Defaults FinalFee
        // to Batch.CourseFee. Called automatically when a student is approved
        // and assigned to a batch.
        Task<(bool Success, string Message, StudentFeeAccount? Account)> EnsureAccountAsync(
            Guid studentId, Guid batchId, Guid? actorId);

        Task<StudentFeeAccount?> GetAccountAsync(Guid studentId, Guid batchId);
        Task<IList<StudentFeeAccount>> GetForBatchAsync(Guid batchId);
        Task<IList<StudentFeeAccount>> GetForStudentAsync(Guid studentId);

        Task<(bool Success, string Message, FeePayment? Payment)> RecordPaymentAsync(
            Guid accountId, decimal amount, DateTime paidOn,
            PaymentMethod method, string? receiptNumber, string? note, Guid? recordedBy);

        // Apply a discount (currency or % depending on caller). Recomputes
        // FinalFee and Status. discountAmount is absolute currency.
        Task<(bool Success, string Message)> ApplyDiscountAsync(
            Guid accountId, decimal discountAmount, string? reason, Guid? actorId);

        // True when the student has cleared at least Batch.MinimumEnrollment.
        Task<bool> IsExamAdmitEligibleAsync(Guid studentId, Guid batchId);
    }

    public interface ISalaryService
    {
        Task<IList<TeacherSalaryPayment>> GetForTeacherAsync(Guid teacherId);
        Task<IList<TeacherSalaryPayment>> GetAllAsync(int? year = null, int? month = null);

        Task<(bool Success, string Message, TeacherSalaryPayment? Payment)> RecordAsync(
            Guid teacherId, DateTime periodMonth, decimal amount, DateTime paidOn,
            PaymentMethod method, string? reference, string? note, Guid? recordedBy);

        Task<(bool Success, string Message)> DeleteAsync(Guid id, Guid? actorId);
    }
}
