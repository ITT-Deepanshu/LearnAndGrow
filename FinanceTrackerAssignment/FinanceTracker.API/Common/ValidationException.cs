namespace FinanceTracker.API.Common
{
    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message) { }
    }
}
