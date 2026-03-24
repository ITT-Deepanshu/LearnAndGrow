using ATMApplication.Models;
using ATMApplication.Exceptions;

namespace ATMApplication.Validators
{
    public class WithdrawalValidator
    {
        public void Validate(Device device, double balance, double amount)
        {
            if (device.IsLocked)
                throw new DeviceLockedException();

            if (!device.IsConnected)
                throw new NetworkConnectionException();

            if (balance < amount)
                throw new InsufficientFundsException();
        }
    }
}