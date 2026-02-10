using System;
using Online_Banking_System.Models;
using Online_Banking_System.Repository;
using static Enums;

namespace Online_Banking_System.Services
{
    public class LoanService
    {
        private readonly IAccountRepository accountRepository;

        private Account account = new Account();
        private AccountService accountService = new AccountService(new AccountRepository());

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
            var newLoan = new Loan(new LoanRequest
            {
                LoanId = loanId,
                PrincipalAmount = principal,
                TermInMonths = term,
                LoanType = loanType
            });


            AddLoan(newLoan);
            accountService.Deposit(principal);
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

            if (!MakeLoanPayment(loanId, paymentAmount))
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


        public void AddLoan(Loan loan)
        {
            if (loan != null)
                account.Loans.Add(loan);
        }

        public decimal GetTotalLoanBalance()
        {
            decimal total = 0;
            foreach (var loan in account.Loans)
            {
                total += loan.OutstandingBalance;
            }
            return total;
        }

        public bool MakeLoanPayment(string loanId, decimal amount)
        {
            foreach (var loan in account.Loans)
            {
                if (loan.LoanId == loanId)
                {
                    if (amount > account.Balance)
                        return false;

                    if (loan.MakePayment(amount))
                    {
                        account.Balance -= amount;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
