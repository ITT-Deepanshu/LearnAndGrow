using ATMApplication.Controllers;
using ATMApplication.Services;
using ATMApplication.Validators;
using ATMApplication.UI;

class Program
{
    static void Main(string[] args)
    {
        var validator = new WithdrawalValidator();
        var service = new WithdrawalService(validator);
        var controller = new AtmController(service);
        var ui = new ConsoleUI(controller);

        ui.Start();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}