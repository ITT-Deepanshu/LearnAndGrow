using System;
using Online_Banking_System.Services;
using Online_Banking_System.Repository;

namespace Online_Banking_System
{
    class Program
    {
        static void Main(string[] args)
        {
            IAccountRepository repository = new AccountRepository();
            AccountService accountService = new AccountService(repository);
            TransactionService transactionService = new TransactionService(repository);
            LoanService loanService = new LoanService(repository);

            UserInterface ui = new UserInterface(accountService, transactionService, loanService);
            ui.Run();
        }
    }
}
