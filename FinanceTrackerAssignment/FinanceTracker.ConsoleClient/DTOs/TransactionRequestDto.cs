namespace FinanceTracker.ConsoleClient.DTOs
{
    public class TransactionRequestDto
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }
}
