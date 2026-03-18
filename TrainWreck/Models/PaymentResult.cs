namespace TrainWreck.Models
{
    public sealed class PaymentResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        private PaymentResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static PaymentResult Success(decimal amount) =>
            new PaymentResult(true, $"Payment of {amount:C} collected successfully");

        public static PaymentResult InsufficientFunds(decimal amount) =>
            new PaymentResult(false, $"Insufficient funds. Could not collect {amount:C}. Will return later");
    }
}