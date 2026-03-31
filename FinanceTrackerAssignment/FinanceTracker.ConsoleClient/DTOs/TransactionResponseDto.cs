namespace FinanceTracker.ConsoleClient.DTOs
{
    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }

        public string TypeName => Type == 0 ? "Income" : "Expense";
    }
}
