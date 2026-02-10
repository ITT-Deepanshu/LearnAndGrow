using System;
using Online_Banking_System.Models;
using Online_Banking_System.Services;
using static Enums;

namespace Online_Banking_System
{
    public class UserInterface
    {
        private readonly AccountService accountService;
        private readonly TransactionService transactionService;
        private readonly LoanService loanService;

        public UserInterface(AccountService accService, TransactionService transService, LoanService lnService)
        {
            accountService = accService;
            transactionService = transService;
            loanService = lnService;
        }

        public void Run()
        {
            Console.WriteLine("Online Banking System");

            while (true)
            {
                ShowMenu();
                
                try
                {
                    int choice = Convert.ToInt32(Console.ReadLine());
                    
                    switch (choice)
                    {
                        case 1:
                            CheckBalance();
                            break;

                        case 2:
                            MakeDeposit();
                            break;

                        case 3:
                            MakeWithdrawal();
                            break;

                        case 4:
                            BalanceTransfer();
                            break;

                        case 5:
                            ViewHistory();
                            break;

                        case 6:
                            AddAccount();
                            break;

                        case 7:
                            SearchAccount();
                            break;

                        case 8:
                            ApplyForLoan();
                            break;

                        case 9:
                            MakeLoanPayment();
                            break;

                        case 10:
                            ViewLoans();
                            break;

                        case 11:
                            Console.WriteLine("Thank you for using Online Banking System!");
                            return;

                        default:
                            Console.WriteLine("Enter a valid choice!");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid input! Please enter a number.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine("\n===== Online Banking System =====");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Check Balance");
            Console.WriteLine("2. Deposit");
            Console.WriteLine("3. Withdrawal");
            Console.WriteLine("4. Balance Transfer");
            Console.WriteLine("5. View Transaction History");
            Console.WriteLine("6. Add New Account");
            Console.WriteLine("7. Search Account");
            Console.WriteLine("8. Apply for Loan");
            Console.WriteLine("9. Make Loan Payment");
            Console.WriteLine("10. View Loans");
            Console.WriteLine("11. Exit");
            Console.Write("Enter your choice: ");
        }

        private void CheckBalance()
        {
            Console.Write("Enter the account number: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Account account = accountService.GetAccount(accountNumber);
                
                Console.WriteLine("\n========== Account Details ==========");
                Console.WriteLine($"Account Number: {account.AccountNumber}");
                Console.WriteLine($"Customer Name: {account.Customer.Name}");
                Console.WriteLine($"Current Balance: {account.Balance:F2}");
                
                if (account.Loans.Count > 0)
                {
                    Console.WriteLine("\n--- Running Loans ---");
                    decimal totalEMI = 0;
                    foreach (var loan in account.Loans)
                    {
                        if (!loan.IsFullyPaid())
                        {
                            decimal emi = loan.CalculateMonthlyEMI();
                            totalEMI += emi;
                            Console.WriteLine($"Loan Type: {loan.Type}");
                            Console.WriteLine($"  Sanctioned: {loan.PrincipalAmount:F2}");
                            Console.WriteLine($"  Outstanding: {loan.OutstandingBalance:F2}");
                            Console.WriteLine($"  Monthly EMI: {emi:F2}");
                            Console.WriteLine($"  Interest Rate: {loan.InterestRate}%");
                            Console.WriteLine();
                        }
                    }
                    Console.WriteLine($"Total Monthly EMI: {totalEMI:F2}");
                }
                else
                {
                    Console.WriteLine("\nNo active loans.");
                }
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void MakeDeposit()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            Console.Write("Enter amount to deposit: ");
            string amountInput = Console.ReadLine();

            try
            {
                decimal amount = Convert.ToDecimal(amountInput);
                transactionService.Deposit(accountNumber, amount);
                
                Account account = accountService.GetAccount(accountNumber);
                Console.WriteLine($"Deposited {amount:F2} successfully!");
                Console.WriteLine($"New balance: {account.Balance:F2}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid amount format!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void MakeWithdrawal()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            Console.Write("Enter amount to withdraw: ");
            string amountInput = Console.ReadLine();

            try
            {
                decimal amount = Convert.ToDecimal(amountInput);
                transactionService.Withdraw(accountNumber, amount);
                
                Account account = accountService.GetAccount(accountNumber);
                Console.WriteLine($"Withdrawn {amount:F2} successfully!");
                Console.WriteLine($"Remaining balance: {account.Balance:F2}");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid amount format!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void BalanceTransfer()
        {
            Console.Write("Enter sender account number: ");
            string senderAccount = Console.ReadLine();

            Console.Write("Enter receiver account number: ");
            string receiverAccount = Console.ReadLine();

            Console.Write("Enter amount to transfer: ");
            string amountInput = Console.ReadLine();

            try
            {
                decimal amount = Convert.ToDecimal(amountInput);
                transactionService.Transfer(senderAccount, receiverAccount, amount);
                Console.WriteLine("Transaction Successful!");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid amount format!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ViewHistory()
        {
            Console.Write("Enter the account number: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Account account = accountService.GetAccount(accountNumber);
                Console.WriteLine("Transaction History:");
                foreach (var transaction in account.Transactions)
                {
                    Console.WriteLine($"{transaction.Date} UTC, {transaction.Description}, {transaction.Amount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void AddAccount()
        {
            Console.Write("Enter customer name: ");
            string customerName = Console.ReadLine();

            Console.Write("Enter phone number: ");
            string phoneNumber = Console.ReadLine();

            Console.Write("Enter email: ");
            string email = Console.ReadLine();

            try
            {
                string accountNumber = accountService.CreateAccount(customerName, phoneNumber, email);
                Console.WriteLine("\nAccount Created Successfully!");
                Console.WriteLine($"Your Account Number: {accountNumber}");
                Console.WriteLine($"Initial Balance: 5000.00");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void SearchAccount()
        {
            Console.Write("Enter account number to search: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Account account = accountService.GetAccount(accountNumber);
                
                Console.WriteLine("\n========== Account Found ==========");
                Console.WriteLine($"Account Number: {account.AccountNumber}");
                Console.WriteLine($"Customer Name: {account.Customer.Name}");
                Console.WriteLine($"Phone Number: {account.Customer.PhoneNumber}");
                Console.WriteLine($"Email: {account.Customer.Email}");
                Console.WriteLine($"Account Type: {account.AccountType}");
                Console.WriteLine($"Current Balance: {account.Balance:F2}");
                
                if (account.Loans.Count > 0)
                {
                    Console.WriteLine($"\nTotal Active Loans: {account.Loans.Count}");
                    Console.WriteLine($"Total Outstanding: {loanService.GetTotalLoanBalance():F2}");
                }
                else
                {
                    Console.WriteLine("\nNo active loans.");
                }
                
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ApplyForLoan()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Console.WriteLine("\n===== Available Loan Types =====");
                Console.WriteLine("1. Personal Loan (Interest Rate: 12.5%)");
                Console.WriteLine("2. Home Loan (Interest Rate: 8.5%)");
                Console.WriteLine("3. Car Loan (Interest Rate: 10.0%)");
                Console.Write("\nSelect Loan Type: ");
                int typeChoice = Convert.ToInt32(Console.ReadLine());

                LoanType loanType;
                switch (typeChoice)
                {
                    case 1:
                        loanType = LoanType.Personal;
                        break;
                    case 2:
                        loanType = LoanType.Home;
                        break;
                    case 3:
                        loanType = LoanType.Car;
                        break;
                    default:
                        Console.WriteLine("Invalid loan type!");
                        return;
                }

                decimal fixedRate = Loan.GetInterestRate(loanType);

                Console.Write("Enter loan amount: ");
                decimal principal = Convert.ToDecimal(Console.ReadLine());

                Console.Write("Enter loan term (in months): ");
                int term = Convert.ToInt32(Console.ReadLine());

                Loan newLoan = loanService.ApplyForLoan(accountNumber, principal, term, loanType);

                Account account = accountService.GetAccount(accountNumber);

                Console.WriteLine("\n===== Loan Approved Successfully! =====");
                Console.WriteLine($"Loan ID: {newLoan.LoanId}");
                Console.WriteLine($"Loan Type: {newLoan.Type}");
                Console.WriteLine($"Sanctioned Amount: {newLoan.PrincipalAmount:F2}");
                Console.WriteLine($"Interest Rate: {newLoan.InterestRate}% per annum");
                Console.WriteLine($"Loan Term: {newLoan.TermInMonths} months");
                Console.WriteLine($"Monthly EMI: {newLoan.CalculateMonthlyEMI():F2}");
                Console.WriteLine($"Total Amount to Repay: {newLoan.OutstandingBalance:F2}");
                Console.WriteLine($"\nLoan amount {principal:F2} has been added to your account.");
                Console.WriteLine($"Current Account Balance: {account.Balance:F2}");
                Console.WriteLine("========================================");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid input format!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void MakeLoanPayment()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Account account = accountService.GetAccount(accountNumber);

                if (account.Loans.Count == 0)
                {
                    Console.WriteLine("No active loans found!");
                    return;
                }

                Console.WriteLine("\n===== Active Loans =====");
                foreach (var loan in account.Loans)
                {
                    if (!loan.IsFullyPaid())
                    {
                        Console.WriteLine($"\nLoan ID: {loan.LoanId}");
                        Console.WriteLine($"  Type: {loan.Type}");
                        Console.WriteLine($"  Sanctioned Amount: {loan.PrincipalAmount:F2}");
                        Console.WriteLine($"  Outstanding Balance: {loan.OutstandingBalance:F2}");
                        Console.WriteLine($"  Monthly EMI: {loan.CalculateMonthlyEMI():F2}");
                        Console.WriteLine($"  Interest Rate: {loan.InterestRate}%");
                    }
                }

                Console.Write("\nEnter Loan ID to pay EMI: ");
                string loanId = Console.ReadLine();

                Loan selectedLoan = null;
                foreach (var loan in account.Loans)
                {
                    if (loan.LoanId == loanId)
                    {
                        selectedLoan = loan;
                        break;
                    }
                }

                if (selectedLoan == null)
                {
                    Console.WriteLine("Loan ID not found!");
                    return;
                }
                decimal emiAmount = selectedLoan.CalculateMonthlyEMI();

                Console.WriteLine($"\nMonthly EMI for this loan: {emiAmount:F2}");
                Console.Write("Do you want to pay (1) EMI amount or (2) Custom amount? ");
                int paymentChoice = Convert.ToInt32(Console.ReadLine());

                decimal paymentAmount;
                if (paymentChoice == 1)
                {
                    paymentAmount = emiAmount;
                }
                else if (paymentChoice == 2)
                {
                    Console.Write("Enter payment amount: ");
                    paymentAmount = Convert.ToDecimal(Console.ReadLine());
                }
                else
                {
                    Console.WriteLine("Invalid choice!");
                    return;
                }

                loanService.MakeLoanPayment(accountNumber, selectedLoan.LoanId, paymentAmount);

                Console.WriteLine($"\n===== Payment Successful! =====");
                Console.WriteLine($"Amount Paid: {paymentAmount:F2}");
                Console.WriteLine($"Remaining Loan Balance: {selectedLoan.OutstandingBalance:F2}");
                Console.WriteLine($"Current Account Balance: {account.Balance:F2}");

                if (selectedLoan.IsFullyPaid())
                {
                    Console.WriteLine("\n🎉 Congratulations! Loan fully paid!");
                }
                Console.WriteLine("===============================");
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid input format!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ViewLoans()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            try
            {
                Account account = accountService.GetAccount(accountNumber);

                if (account.Loans.Count == 0)
                {
                    Console.WriteLine("No loans found for this account.");
                    return;
                }

                Console.WriteLine("\n========== Loan Details ==========");
                foreach (var loan in account.Loans)
                {
                    Console.WriteLine($"\nLoan ID: {loan.LoanId}");
                    Console.WriteLine($"Type: {loan.Type}");
                    Console.WriteLine($"Sanctioned Amount: {loan.PrincipalAmount:F2}");
                    Console.WriteLine($"Interest Rate: {loan.InterestRate}% per annum");
                    Console.WriteLine($"Loan Term: {loan.TermInMonths} months");
                    Console.WriteLine($"Monthly EMI: {loan.CalculateMonthlyEMI():F2}");
                    Console.WriteLine($"Outstanding Balance: {loan.OutstandingBalance:F2}");
                    Console.WriteLine($"Date Issued: {loan.DateIssued}");
                    Console.WriteLine($"Status: {(loan.IsFullyPaid() ? "Fully Paid" : "Active")}");
                    Console.WriteLine("----------------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
