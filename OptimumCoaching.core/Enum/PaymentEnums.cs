namespace OptimumCoaching.core
{
    public enum PaymentMethod
    {
        Cash = 0,
        BankTransfer = 1,
        bKash = 2,
        Nagad = 3,
        Rocket = 4,
        Card = 5,
        Cheque = 6,
        Other = 99
    }

    public enum FeeAccountStatus
    {
        Unpaid = 0,
        PartiallyPaid = 1,
        PaidInFull = 2,
        Overdue = 3,
        Waived = 4
    }

    public enum SalaryPaymentStatus
    {
        Draft = 0,
        Paid = 1
    }
}
