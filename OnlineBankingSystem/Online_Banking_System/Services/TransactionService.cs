using System;
using Online_Banking_System.Models;
using Online_Banking_System.Repository;
using static Enums;

namespace Online_Banking_System.Services
{
    public class TransactionService
    {
        private readonly IAccountRepository accountRepository;
        private AccountService accountService = new AccountService(new AccountRepository());

        public TransactionService(IAccountRepository repository)
        {
            accountRepository = repository;
        }

        public void Deposit(string accountNumber, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive!");

            Account account = accountRepository.GetAccount(accountNumber);
            accountService.Deposit(amount);
            account.Transactions.Add(new Transaction(TransactionType.Deposit, "Deposit", amount));
        }

        public void Withdraw(string accountNumber, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive!");

            Account account = accountRepository.GetAccount(accountNumber);
            
            if (!accountService.Withdraw(amount))
                throw new InvalidOperationException("Insufficient Balance!");

            account.Transactions.Add(new Transaction(TransactionType.Withdrawal, "Withdrawal", -amount));
        }

        public void Transfer(string senderAccountNumber, string receiverAccountNumber, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(senderAccountNumber) || string.IsNullOrWhiteSpace(receiverAccountNumber))
                throw new ArgumentException("Account numbers cannot be empty!");

            if (senderAccountNumber == receiverAccountNumber)
                throw new InvalidOperationException("Cannot transfer to the same account!");

            if (amount <= 0)
                throw new ArgumentException("Transfer amount must be positive!");

            Account sender = accountRepository.GetAccount(senderAccountNumber);
            Account receiver = accountRepository.GetAccount(receiverAccountNumber);

            if (sender.Balance < amount)
                throw new InvalidOperationException("Insufficient Balance!");

            if (!accountService.Withdraw(amount))
                throw new InvalidOperationException("Insufficient Balance!");

            sender.Transactions.Add(new Transaction(TransactionType.Transfer, 
                "Transfer to " + receiverAccountNumber, -amount));

            accountService.Deposit(amount);
            receiver.Transactions.Add(new Transaction(TransactionType.Transfer, 
                "Transfer from " + senderAccountNumber, amount));
        }
    }
}
