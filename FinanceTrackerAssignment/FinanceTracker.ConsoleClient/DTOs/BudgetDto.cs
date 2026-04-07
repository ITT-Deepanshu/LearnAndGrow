namespace FinanceTracker.ConsoleClient.DTOs
{
    public class BudgetDto
    {
        public Guid UserId { get; set; }
        public string Category { get; set; }
        public decimal Limit { get; set; }
    }
}
