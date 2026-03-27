using System;
using Online_Banking_System.Exceptions;
using static Enums;

namespace Online_Banking_System.Models
{
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
                    return ConstantData.PersonalLoanInterestRate;
                case LoanType.Home:
                    return ConstantData.HomeLoanInterestRate;
                case LoanType.Car:
                    return ConstantData.CarLoanInterestRate;
                default:
                    return ConstantData.DefaultLoanInterestRate;
            }
        }

        public Loan(LoanRequest request)
        {
            LoanId = request.LoanId;
            PrincipalAmount = request.PrincipalAmount;
            TermInMonths = request.TermInMonths;
            Type = request.LoanType;

            InterestRate = GetInterestRate(request.LoanType);
            DateIssued = DateTime.UtcNow;
            OutstandingBalance = CalculateTotalAmount();
        }


        public decimal CalculateMonthlyEMI()
        {
            if (TermInMonths == 0) return 0;

            decimal monthlyRate = CalculateMonthlyRate(InterestRate);

            if (monthlyRate == 0) 
                return PrincipalAmount / TermInMonths;

            decimal emi = PrincipalAmount * monthlyRate * 
                         (decimal)Math.Pow((double)(1 + monthlyRate), TermInMonths) /
                         ((decimal)Math.Pow((double)(1 + monthlyRate), TermInMonths) - 1);
            
            return Math.Round(emi, 2);
        }

        public bool MakePayment(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidAmountException(amount, "Payment amount must be positive!");

            if (amount > OutstandingBalance)
                throw new InvalidAmountException(amount, "Payment amount exceeds outstanding balance!");

            OutstandingBalance -= amount;
            return true;
        }

        public bool IsFullyPaid()
        {
            return OutstandingBalance <= 0;
        }
        private decimal CalculateTotalAmount()
        {
            decimal monthlyRate = CalculateMonthlyRate(InterestRate);
            decimal totalAmount = PrincipalAmount * (1 + (monthlyRate * TermInMonths));
            return totalAmount;
        }
        private decimal CalculateMonthlyRate(decimal InterestRate)
        {
            return InterestRate / 12 / 100;
        }

    }
}
