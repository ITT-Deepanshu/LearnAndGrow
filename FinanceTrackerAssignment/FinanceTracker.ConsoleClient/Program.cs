using Microsoft.Extensions.Configuration;
using FinanceTracker.ConsoleClient.Interfaces;
using FinanceTracker.ConsoleClient.Services;
using FinanceTracker.ConsoleClient.UI;

class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        IApiService apiService = new ApiService(config);
        var menu = new MainMenu(apiService);

        await menu.Start();
    }
}
