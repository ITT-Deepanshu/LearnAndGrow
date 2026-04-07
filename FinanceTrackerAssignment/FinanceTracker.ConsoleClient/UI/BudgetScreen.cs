using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Interfaces;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class BudgetScreen
    {
        private readonly IApiService _api;
        private readonly UserSession _session;

        public BudgetScreen(IApiService api, UserSession session)
        {
            _api = api;
            _session = session;
        }

        public async Task Show()
        {
            Console.WriteLine("\n1. Set Budget\n2. View Budgets");
            var input = Console.ReadLine();

            if (input == "1")
            {
                var dto = new BudgetDto();
                dto.UserId = _session.UserId;

                Console.Write("Category: ");
                dto.Category = Console.ReadLine() ?? string.Empty;

                Console.Write("Limit: ");
                if (!decimal.TryParse(Console.ReadLine(), out var limit))
                {
                    Console.WriteLine("Invalid limit amount. Budget not saved.");
                    return;
                }
                dto.Limit = limit;

                await _api.PostAsync("budgets", dto);

                Console.WriteLine("Budget set.");
            }
            else if (input == "2")
            {
                var data = await _api.GetAsync<List<BudgetDto>>($"budgets?userId={_session.UserId}");

                if (data.Count == 0)
                {
                    Console.WriteLine("No budgets found.");
                    return;
                }

                Console.WriteLine($"\n{"Category",-20}{"Limit",10}");
                Console.WriteLine(new string('-', 32));
                foreach (var b in data)
                {
                    Console.WriteLine($"{b.Category,-20}{b.Limit,10:F2}");
                }
            }
            else
            {
                Console.WriteLine("Invalid option.");
            }
        }
    }
}