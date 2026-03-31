namespace FinanceTracker.API.Common
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message) { }
    }
}
