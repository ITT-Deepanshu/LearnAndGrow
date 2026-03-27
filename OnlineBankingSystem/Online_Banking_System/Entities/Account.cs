using System.Collections.Generic;

namespace Online_Banking_System.Models
{
    public class Account
    {
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public Customer Customer { get; set; }
        public string AccountType { get; set; }
        public List<Transaction> Transactions { get; }
        public List<Loan> Loans { get; }

        public Account()
        {
            Balance = ConstantData.SavingAccountDefaultBalance;
            AccountType = ConstantData.SavingAccountType;
            Transactions = new List<Transaction>();
            Loans = new List<Loan>();
        }
    }
}
