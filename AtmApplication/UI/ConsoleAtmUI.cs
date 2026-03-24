using ATMApplication.Controllers;
using ATMApplication.Models;

namespace ATMApplication.UI
{
    public class ConsoleUI
    {
        private readonly AtmController _controller;

        public ConsoleUI(AtmController controller)
        {
            _controller = controller;
        }

        public void Start()
        {
            var account = new Account
            {
                Id = "111",
                Balance = 5000
            };

            var device = new Device
            {
                IsLocked = false,
                IsConnected = true
            };

            Console.WriteLine("===== ATM SYSTEM =====");

            while (true)
            {
                Console.WriteLine("\n1. Withdraw Cash");
                Console.WriteLine("2. Check Balance");
                Console.WriteLine("3. Exit");

                Console.Write("Select option: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        double amount = ReadAmount();
                        _controller.Withdraw(account, device, amount);
                        break;

                    case "2":
                        Console.WriteLine($"Current Balance: {account.Balance}");
                        break;

                    case "3":
                        Console.WriteLine("Thank you! ");
                        return; 

                    default:
                        Console.WriteLine("Invalid option ");
                        break;
                }
            }
        }

        private double ReadAmount()
        {
            while (true)
            {
                Console.Write("Enter amount: ");
                var input = Console.ReadLine();

                if (double.TryParse(input, out double amount) && amount > 0)
                {
                    return amount;
                }

                Console.WriteLine("Invalid input, Please enter a valid amount.");
            }
        }
    }
}