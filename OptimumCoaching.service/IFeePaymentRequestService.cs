using OptimumCoaching.core;

namespace OptimumCoaching.service
{
    public interface IFeePaymentRequestService
    {
        // Student submits a payment for a specific fee account. The receipt
        // image path is stored verbatim — caller saves the upload first.
        Task<(bool Success, string Message, FeePaymentRequest? Request)> SubmitAsync(
            Guid accountId, Guid submittedByUserId,
            decimal amount, PaymentMethod method,
            string? transactionReference, string? receiptImagePath, string? note);

        Task<IList<FeePaymentRequest>> GetForStudentUserAsync(Guid userId);
        Task<IList<FeePaymentRequest>> GetForAccountAsync(Guid accountId);
        Task<IList<FeePaymentRequest>> GetPendingAsync();
        Task<FeePaymentRequest?> GetByIdAsync(Guid id);
        Task<int> GetPendingCountAsync();

        // Approves a pending request — records a real FeePayment on the account
        // and links it back to the request.
        Task<(bool Success, string Message)> ApproveAsync(
            Guid requestId, Guid? reviewerUserId);

        Task<(bool Success, string Message)> RejectAsync(
            Guid requestId, string? reason, Guid? reviewerUserId);
    }
}
