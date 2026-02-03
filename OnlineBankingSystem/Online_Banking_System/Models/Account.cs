using System.Collections.Generic;

namespace Online_Banking_System.Models
{
    public class Account
    {
        public string AccountNumber { get; set; }
        public decimal Balance { get; private set; }
        public Customer Customer { get; set; }
        public string AccountType { get; set; }
        public List<Transaction> Transactions { get; }
        public List<Loan> Loans { get; }

        public Account()
        {
            Balance = 5000;
            AccountType = "Savings";
            Transactions = new List<Transaction>();
            Loans = new List<Loan>();
        }

        public Account(decimal initialBalance, string accountType)
        {
            Balance = initialBalance;
            AccountType = accountType;
            Transactions = new List<Transaction>();
            Loans = new List<Loan>();
        }

        public void Deposit(decimal amount)
        {
            if (amount > 0)
                Balance += amount;
        }

        public bool Withdraw(decimal amount)
        {
            if (amount > 0 && amount <= Balance)
            {
                Balance -= amount;
                return true;
            }
            return false;
        }

        public void AddLoan(Loan loan)
        {
            if (loan != null)
                Loans.Add(loan);
        }

        public decimal GetTotalLoanBalance()
        {
            decimal total = 0;
            foreach (var loan in Loans)
            {
                total += loan.OutstandingBalance;
            }
            return total;
        }

        public bool MakeLoanPayment(string loanId, decimal amount)
        {
            foreach (var loan in Loans)
            {
                if (loan.LoanId == loanId)
                {
                    if (amount > Balance)
                        return false;
                    
                    if (loan.MakePayment(amount))
                    {
                        Balance -= amount;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
