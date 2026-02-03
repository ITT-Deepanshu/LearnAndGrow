using System;

namespace Online_Banking_System.Models
{
    public enum LoanType
    {
        Personal,
        Home,
        Car
    }

    public class Loan
    {
        public string LoanId { get; }
        public decimal PrincipalAmount { get; }
        public decimal InterestRate { get; }
        public int TermInMonths { get; }
        public decimal OutstandingBalance { get; private set; }
        public LoanType Type { get; }
        public DateTime DateIssued { get; }

        public static decimal GetInterestRate(LoanType loanType)
        {
            switch (loanType)
            {
                case LoanType.Personal:
                    return 12.5m;
                case LoanType.Home:
                    return 8.5m;
                case LoanType.Car:
                    return 10.0m;
                default:
                    return 10.0m;
            }
        }

        public Loan(string id, decimal principal, decimal rate, int term, LoanType loanType)
        {
            LoanId = id;
            PrincipalAmount = principal;
            InterestRate = rate;
            TermInMonths = term;
            Type = loanType;
            DateIssued = DateTime.UtcNow;
            OutstandingBalance = CalculateTotalAmount();
        }

        private decimal CalculateTotalAmount()
        {
            decimal monthlyRate = InterestRate / 12 / 100;
            decimal totalAmount = PrincipalAmount * (1 + (monthlyRate * TermInMonths));
            return totalAmount;
        }

        public decimal CalculateMonthlyEMI()
        {
            if (TermInMonths == 0) return 0;

            decimal monthlyRate = InterestRate / 12 / 100;
            
            if (monthlyRate == 0) 
                return PrincipalAmount / TermInMonths;

            decimal emi = PrincipalAmount * monthlyRate * 
                         (decimal)Math.Pow((double)(1 + monthlyRate), TermInMonths) /
                         ((decimal)Math.Pow((double)(1 + monthlyRate), TermInMonths) - 1);
            
            return Math.Round(emi, 2);
        }

        public bool MakePayment(decimal amount)
        {
            if (amount <= 0 || amount > OutstandingBalance)
                return false;

            OutstandingBalance -= amount;
            return true;
        }

        public bool IsFullyPaid()
        {
            return OutstandingBalance <= 0;
        }
    }
}
