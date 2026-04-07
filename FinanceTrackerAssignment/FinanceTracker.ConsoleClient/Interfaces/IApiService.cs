namespace FinanceTracker.ConsoleClient.Interfaces
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string url);
        Task PostAsync<T>(string url, T data);
        Task DeleteAsync(string url);
    }
}
