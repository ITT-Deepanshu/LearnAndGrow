using ATMApplication.Interfaces;
using ATMApplication.Models;
using ATMApplication.Validators;

namespace ATMApplication.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly WithdrawalValidator _validator;

        public WithdrawalService(WithdrawalValidator validator)
        {
            _validator = validator;
        }

        public void Withdraw(Account account, Device device, double amount)
        {
            _validator.Validate(device, account.Balance, amount);

            account.Balance -= amount;
            device.DispenseCash(amount);
        }
    }
}