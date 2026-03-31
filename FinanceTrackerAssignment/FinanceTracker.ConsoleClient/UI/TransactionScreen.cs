using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class TransactionScreen
    {
        private readonly ApiService _api;
        private readonly UserSession _session;

        public TransactionScreen(ApiService api, UserSession session)
        {
            _api = api;
            _session = session;
        }

        public async Task Show()
        {
            Console.WriteLine("1. Add 2. View 3. Delete");
            var input = Console.ReadLine();

            if (input == "1")
            {
                var dto = new TransactionRequestDto();

                Console.Write("Type (Income/Expense): ");
                dto.Type = Console.ReadLine();

                Console.Write("Amount: ");
                dto.Amount = decimal.Parse(Console.ReadLine());

                Console.Write("Category: ");
                dto.Category = Console.ReadLine();

                await _api.PostAsync("transactions", new
                {
                    userId = _session.UserId,
                    type = dto.Type,
                    amount = dto.Amount,
                    category = dto.Category
                });

                Console.WriteLine("Transaction Added");
            }
            else if (input == "2")
            {
                var transactions = await _api.GetAsync<List<TransactionResponseDto>>($"transactions?userId={_session.UserId}");

                if (transactions.Count == 0)
                {
                    Console.WriteLine("No transactions found.");
                    return;
                }

                Console.WriteLine($"\n{"Type",-10}{"Amount",10}  {"Category",-15}{"Date",-22}{"Id"}");
                Console.WriteLine(new string('-', 85));

                foreach (var t in transactions)
                {
                    Console.WriteLine($"{t.TypeName,-10}{t.Amount,10:F2}  {t.Category,-15}{t.Date:yyyy-MM-dd HH:mm,-22}{t.Id}");
                }
            }
            else if (input == "3")
            {
                Console.Write("Enter Id: ");
                var id = Console.ReadLine();

                await _api.DeleteAsync($"transactions/{id}");

                Console.WriteLine("Deleted");
            }
        }
    }
}