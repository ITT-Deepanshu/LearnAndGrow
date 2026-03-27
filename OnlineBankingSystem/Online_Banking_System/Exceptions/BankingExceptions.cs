using System;

namespace Online_Banking_System.Exceptions
{
    // Base exception for all banking-related errors
    public class BankingException : Exception
    {
        public BankingException(string message) : base(message) { }
        public BankingException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Thrown when an account is not found in the repository
    public class AccountNotFoundException : BankingException
    {
        public string AccountNumber { get; }

        public AccountNotFoundException(string accountNumber)
            : base($"Account '{accountNumber}' not found!")
        {
            AccountNumber = accountNumber;
        }
    }

    // Thrown when account balance is insufficient for an operation
    public class InsufficientBalanceException : BankingException
    {
        public decimal RequiredAmount { get; }
        public decimal AvailableBalance { get; }

        public InsufficientBalanceException(decimal requiredAmount, decimal availableBalance)
            : base($"Insufficient balance! Required: {requiredAmount:F2}, Available: {availableBalance:F2}")
        {
            RequiredAmount = requiredAmount;
            AvailableBalance = availableBalance;
        }
    }

    // Thrown when a transaction amount is invalid (zero, negative, etc.)
    public class InvalidAmountException : BankingException
    {
        public decimal Amount { get; }

        public InvalidAmountException(decimal amount, string reason)
            : base($"Invalid amount {amount:F2}: {reason}")
        {
            Amount = amount;
        }
    }

    // Thrown when account details (name, phone, email, account number) are invalid
    public class InvalidAccountDetailsException : BankingException
    {
        public string FieldName { get; }

        public InvalidAccountDetailsException(string fieldName, string reason)
            : base($"Invalid {fieldName}: {reason}")
        {
            FieldName = fieldName;
        }
    }

    // Thrown when a loan is not found
    public class LoanNotFoundException : BankingException
    {
        public string LoanId { get; }

        public LoanNotFoundException(string loanId)
            : base($"Loan '{loanId}' not found!")
        {
            LoanId = loanId;
        }
    }

    // Thrown when loan parameters (amount, term) are invalid
    public class InvalidLoanParameterException : BankingException
    {
        public string ParameterName { get; }

        public InvalidLoanParameterException(string parameterName, string reason)
            : base($"Invalid loan {parameterName}: {reason}")
        {
            ParameterName = parameterName;
        }
    }

    // Thrown when a transfer is attempted to the same account
    public class SameAccountTransferException : BankingException
    {
        public SameAccountTransferException(string accountNumber)
            : base($"Cannot transfer to the same account '{accountNumber}'!")
        {
        }
    }

    // Thrown when a loan payment operation fails
    public class LoanPaymentException : BankingException
    {
        public string LoanId { get; }

        public LoanPaymentException(string loanId, string reason)
            : base($"Loan payment failed for '{loanId}': {reason}")
        {
            LoanId = loanId;
        }
    }
}
