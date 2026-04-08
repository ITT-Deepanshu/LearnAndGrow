using GeoLocatorApp.Interfaces;

namespace GeoLocatorApp.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _client;

        public HttpClientService()
        {
            _client = new HttpClient();
        }

        public async Task<string> GetAsync(string url)
        {
            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP Error: {response.StatusCode}");

            return await response.Content.ReadAsStringAsync();
        }
    }
}
