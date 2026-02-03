using System;
using Online_Banking_System.Models;
using Online_Banking_System.Repository;

namespace Online_Banking_System.Services
{
    public class LoanService
    {
        private readonly IAccountRepository accountRepository;

        public LoanService(IAccountRepository repository)
        {
            accountRepository = repository;
        }

        public Loan ApplyForLoan(string accountNumber, decimal principal, int term, LoanType loanType)
        {
            if (principal <= 0)
                throw new ArgumentException("Loan amount must be positive!");

            if (term <= 0)
                throw new ArgumentException("Loan term must be positive!");

            Account account = accountRepository.GetAccount(accountNumber);

            decimal fixedRate = Loan.GetInterestRate(loanType);
            string loanId = "LN" + DateTime.Now.Ticks;
            Loan newLoan = new Loan(loanId, principal, fixedRate, term, loanType);

            account.AddLoan(newLoan);
            account.Deposit(principal);
            account.Transactions.Add(new Transaction(TransactionType.LoanDisbursement,
                $"Loan Disbursement - {loanType}", principal));

            return newLoan;
        }

        public void MakeLoanPayment(string accountNumber, string loanId, decimal paymentAmount)
        {
            if (paymentAmount <= 0)
                throw new ArgumentException("Payment amount must be positive!");

            Account account = accountRepository.GetAccount(accountNumber);

            Loan loan = FindLoan(account, loanId);
            if (loan == null)
                throw new InvalidOperationException("Loan not found!");

            if (paymentAmount > loan.OutstandingBalance)
                throw new ArgumentException("Payment amount exceeds outstanding balance!");

            if (!account.MakeLoanPayment(loanId, paymentAmount))
                throw new InvalidOperationException("Payment failed! Check account balance or loan details.");

            account.Transactions.Add(new Transaction(TransactionType.LoanPayment,
                $"Loan Payment - {loanId}", -paymentAmount));
        }

        private Loan FindLoan(Account account, string loanId)
        {
            foreach (var loan in account.Loans)
            {
                if (loan.LoanId == loanId)
                    return loan;
            }
            return null;
        }
    }
}
