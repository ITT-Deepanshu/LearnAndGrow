using FinanceTracker.ConsoleClient.Services;
using FinanceTracker.ConsoleClient.UI;

class Program
{
    static async Task Main()
    {
        var apiService = new ApiService();
        var menu = new MainMenu(apiService);

        await menu.Start();
    }
}