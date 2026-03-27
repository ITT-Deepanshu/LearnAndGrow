using System;
using static Enums;

namespace Online_Banking_System.Models
{
   
    public class Transaction
    {
        public DateTime Date { get; }
        public string Description { get; }
        public decimal Amount { get; }
        public TransactionType Type { get; }

        public Transaction(TransactionType type, string description, decimal amount)
        {
            Date = DateTime.UtcNow;
            Type = type;
            Description = description;
            Amount = amount;
        }
    }
}
