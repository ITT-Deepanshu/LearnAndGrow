using Online_Banking_System.Models;

namespace Online_Banking_System.Repository
{
    public interface IAccountRepository
    {
        Account GetAccount(string accountNumber);
        void AddAccount(Account account);
        bool AccountExists(string accountNumber);
    }
}
