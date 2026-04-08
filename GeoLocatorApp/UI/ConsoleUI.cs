using GeoLocatorApp.Handlers;
using GeoLocatorApp.Utils;
using GeoLocatorApp.Validators;

namespace GeoLocatorApp.UI
{
    public class ConsoleUI
    {
        private readonly LocationHandler _handler;

        public ConsoleUI(LocationHandler handler)
        {
            _handler = handler;
        }

        public async Task RunAsync()
        {
            ConsoleHelper.PrintHeader();

            Console.Write("Enter location: ");
            var input = Console.ReadLine();

            if (!InputValidator.IsValid(input))
            {
                ConsoleHelper.PrintError("Invalid input.");
                return;
            }

            try
            {
                var results = await _handler.HandleAsync(input);

                if (!results.Any())
                {
                    ConsoleHelper.PrintError("No results found.");
                    return;
                }

                ConsoleHelper.PrintSuccess("Results:\n");

                foreach (var result in results)
                {
                    Console.WriteLine($"Address   : {result.Address}");
                    Console.WriteLine($"Latitude  : {result.Latitude}");
                    Console.WriteLine($"Longitude : {result.Longitude}");
                    Console.WriteLine(new string('-', 40));
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.PrintError(ex.Message);
            }
        }
    }
}
