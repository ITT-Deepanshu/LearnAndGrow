using System;

namespace ATMApplication.Exceptions
{
    public class InsufficientFundsException : Exception
    {
        public InsufficientFundsException() : base("Insufficient balance.") { }
    }
}