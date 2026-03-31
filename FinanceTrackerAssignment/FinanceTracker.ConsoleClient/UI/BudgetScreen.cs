using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class BudgetScreen
    {
        private readonly ApiService _api;
        private readonly UserSession _session;

        public BudgetScreen(ApiService api, UserSession session)
        {
            _api = api;
            _session = session;
        }

        public async Task Show()
        {
            Console.WriteLine("1. Set Budget 2. View Budgets");
            var input = Console.ReadLine();

            if (input == "1")
            {
                var dto = new BudgetDto();
                dto.UserId = _session.UserId;

                Console.Write("Category: ");
                dto.Category = Console.ReadLine();

                Console.Write("Limit: ");
                dto.Limit = decimal.Parse(Console.ReadLine());

                await _api.PostAsync("budgets", dto);

                Console.WriteLine("Budget Set");
            }
            else if (input == "2")
            {
                var data = await _api.GetAsync<List<BudgetDto>>($"budgets?userId={_session.UserId}");

                if (data.Count == 0)
                {
                    Console.WriteLine("No budgets found.");
                    return;
                }

                foreach (var b in data)
                {
                    Console.WriteLine($"Category: {b.Category}, Limit: {b.Limit}");
                }
            }
        }
    }
}