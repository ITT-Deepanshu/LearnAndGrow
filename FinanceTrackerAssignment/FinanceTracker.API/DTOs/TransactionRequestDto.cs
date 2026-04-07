namespace FinanceTracker.API.DTOs
{
    public class TransactionRequestDto
    {
        public Guid UserId { get; set; }  
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }
}
