using System;
using System.Text.RegularExpressions;
using Online_Banking_System.Models;
using Online_Banking_System.Repository;

namespace Online_Banking_System.Services
{
    public class AccountService
    {
        private readonly IAccountRepository accountRepository;
        private static readonly Random random = new Random();

        public AccountService(IAccountRepository repository)
        {
            accountRepository = repository;
        }

        public string CreateAccount(string customerName, string phoneNumber, string email)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name cannot be empty!");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty!");

            if (!IsValidPhoneNumber(phoneNumber))
                throw new ArgumentException("Phone number must be 10 digits!");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty!");

            if (!IsValidEmail(email))
                throw new ArgumentException("Invalid email format!");

            string accountNumber = GenerateAccountNumber();

            Customer customer = new Customer(customerName, phoneNumber, email);
            Account newAccount = new Account 
            { 
                AccountNumber = accountNumber,
                Customer = customer
            };
            
            accountRepository.AddAccount(newAccount);
            return accountNumber;
        }

        private string GenerateAccountNumber()
        {
            string accountNumber;
            do
            {
                accountNumber = "";
                for (int i = 0; i < 15; i++)
                {
                    accountNumber += random.Next(0, 10).ToString();
                }
            } while (accountRepository.AccountExists(accountNumber));

            return accountNumber;
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            string digitsOnly = Regex.Replace(phoneNumber, @"\D", "");
            return digitsOnly.Length == 10;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        public Account GetAccount(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new ArgumentException("Account number cannot be empty!");

            if (!IsValidAccountNumber(accountNumber))
                throw new ArgumentException("Account number must be 15 digits!");

            return accountRepository.GetAccount(accountNumber);
        }

        private bool IsValidAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return false;

            string digitsOnly = Regex.Replace(accountNumber, @"\D", "");
            return digitsOnly.Length == 15;
        }

        public decimal CheckBalance(string accountNumber)
        {
            Account account = GetAccount(accountNumber);
            return account.Balance;
        }
    }
}
