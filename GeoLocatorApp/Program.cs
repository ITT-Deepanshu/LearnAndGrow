using GeoLocatorApp.Handlers;
using GeoLocatorApp.Interfaces;
using GeoLocatorApp.Services;
using Microsoft.Extensions.Configuration;
using GeoLocatorApp.UI;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        IHttpClientService httpClient = new HttpClientService();
        IGeocodingGateway gateway = new LocationIqGeocodingGateway(httpClient, config);

        var handler = new LocationHandler(gateway);
        var ui = new ConsoleUI(handler);

        await ui.RunAsync();
    }
}