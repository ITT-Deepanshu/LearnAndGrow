namespace FinanceTracker.API.Models
{
    public enum TransactionType { Income, Expense }

    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; } 
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
    }
}
