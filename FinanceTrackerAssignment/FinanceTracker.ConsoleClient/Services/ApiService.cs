using System.Text;
using System.Text.Json;
using FinanceTracker.ConsoleClient.Common;
using FinanceTracker.ConsoleClient.Exceptions;

namespace FinanceTracker.ConsoleClient.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7102/")
            };
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

            var result = JsonSerializer.Deserialize<ApiResponse<string>>(resultJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || !result.Success)
                throw new ApiException(result?.Message ?? "API Error");
        }

        public async Task DeleteAsync(string url)
        {
            var response = await _client.DeleteAsync(url);

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ApiResponse<string>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || !result.Success)
                throw new ApiException(result?.Message ?? "API Error");
        }
    }
}