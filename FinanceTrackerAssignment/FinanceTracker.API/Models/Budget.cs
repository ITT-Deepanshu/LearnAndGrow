namespace FinanceTracker.API.Models
{
    public class Budget
    {
        public Guid UserId { get; set; }   
        public string Category { get; set; }
        public decimal Limit { get; set; }
    }
}
