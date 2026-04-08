using GeoLocatorApp.DTOs;
using GeoLocatorApp.Interfaces;
using GeoLocatorApp.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GeoLocatorApp.Services
{
    public class LocationIqGeocodingGateway(IHttpClientService httpClient, IConfiguration config) : IGeocodingGateway
    {
        private readonly IHttpClientService _httpClient = httpClient;
        private readonly string _apiKey = config["LocationIQ:ApiKey"];
        private readonly string _baseUrl = config["LocationIQ:BaseUrl"];

        public async Task<List<LocationResult>> GetCoordinatesAsync(string location)
        {
            var url = BuildUrl(location);

            var rawResponse = await _httpClient.GetAsync(url);

            var response = Deserialize(rawResponse);

            ValidateResponse(response);

            return MapToDomain(response);
        }

        private string BuildUrl(string location)
        {
            return $"{_baseUrl}?key={_apiKey}&q={Uri.EscapeDataString(location)}&format=json";
        }

        private List<LocationIqResult> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Empty response from API.");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<LocationIqResult>>(json, options)
                   ?? new List<LocationIqResult>();
        }

        private void ValidateResponse(List<LocationIqResult> response)
        {
            if (response == null || !response.Any())
                throw new Exception("No results found from LocationIQ.");
        }

        private List<LocationResult> MapToDomain(List<LocationIqResult> response)
        {
            return response.Select(r =>
            {
                double.TryParse(r.Lat, out var lat);
                double.TryParse(r.Lon, out var lon);

                return new LocationResult
                {
                    Address = r.DisplayName,
                    Latitude = lat,
                    Longitude = lon
                };
            }).ToList();
        }
    }
}
