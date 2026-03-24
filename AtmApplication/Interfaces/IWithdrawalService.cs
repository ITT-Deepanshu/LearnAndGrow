using ATMApplication.Models;

namespace ATMApplication.Interfaces
{
    public interface IWithdrawalService
    {
        void Withdraw(Account account, Device device, double amount);
    }
}