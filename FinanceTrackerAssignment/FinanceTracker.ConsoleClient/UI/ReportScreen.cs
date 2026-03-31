using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class ReportScreen
    {
        private readonly ApiService _api;
        private readonly UserSession _session;

        public ReportScreen(ApiService api, UserSession session)
        {
            _api = api;
            _session = session;
        }

        public async Task Show()
        {
            var data = await _api.GetAsync<ReportResponseDto>($"reports/summary?userId={_session.UserId}");

            Console.WriteLine($"Income: {data.Income}");
            Console.WriteLine($"Expense: {data.Expense}");
            Console.WriteLine($"Savings: {data.Savings}");
        }
    }
}