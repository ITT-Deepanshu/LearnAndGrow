using System.Text;
using System.Text.Json;
using FinanceTracker.ConsoleClient.Common;
using FinanceTracker.ConsoleClient.Exceptions;
using FinanceTracker.ConsoleClient.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.ConsoleClient.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ApiService(IConfiguration config)
        {
            var baseUrl = config["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured in appsettings.json.");

            _client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

            if (result == null || !result.Success)
                throw new ApiException(result?.Message ?? "API Error");

            return result.Data;
        }

        public async Task PostAsync<T>(string url, T data)
        {
            var json = JsonSerializer.Serialize(data);

            var response = await _client.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"));

            var resultJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<string>>(resultJson, _jsonOptions);

            if (result == null || !result.Success)
                throw new ApiException(result?.Message ?? "API Error");
        }

        public async Task DeleteAsync(string url)
        {
            var response = await _client.DeleteAsync(url);

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<string>>(json, _jsonOptions);

            if (result == null || !result.Success)
                throw new ApiException(result?.Message ?? "API Error");
        }
    }
}
