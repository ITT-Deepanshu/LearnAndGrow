using ATMApplication.Interfaces;
using ATMApplication.Models;
using ATMApplication.Exceptions;

namespace ATMApplication.Controllers
{
    public class AtmController
    {
        private readonly IWithdrawalService _withdrawalService;

        public AtmController(IWithdrawalService withdrawalalService)
        {
            _withdrawalService = withdrawalalService;
        }

        public void Withdraw(Account account, Device device, double amount)
        {
            try
            {
                _withdrawalService.Withdraw(account, device, amount);
                Console.WriteLine("Withdrawal successful");
            }
            catch (DeviceLockedException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (NetworkConnectionException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InsufficientFundsException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                Console.WriteLine("Something went wrong");
            }
        }
    }
}