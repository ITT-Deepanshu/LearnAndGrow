using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Interfaces;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class TransactionScreen
    {
        private readonly IApiService _api;
        private readonly UserSession _session;

        public TransactionScreen(IApiService api, UserSession session)
        {
            _api = api;
            _session = session;
        }

        public async Task Show()
        {
            Console.WriteLine("\n1. Add\n2. View\n3. Delete");
            var input = Console.ReadLine();

            if (input == "1")
            {
                var dto = new TransactionRequestDto();

                Console.Write("Type (Income/Expense): ");
                dto.Type = Console.ReadLine() ?? string.Empty;

                Console.Write("Amount: ");
                if (!decimal.TryParse(Console.ReadLine(), out var amount))
                {
                    Console.WriteLine("Invalid amount. Transaction cancelled.");
                    return;
                }
                dto.Amount = amount;

                Console.Write("Category: ");
                dto.Category = Console.ReadLine() ?? string.Empty;

                dto.UserId = _session.UserId;

                await _api.PostAsync("transactions", dto);

                Console.WriteLine("Transaction Added.");
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
                    Console.WriteLine($"{t.TypeName,-10}{t.Amount,10:F2}  {t.Category,-15}{t.Date,-22:yyyy-MM-dd HH:mm}{t.Id}");
                }
            }
            else if (input == "3")
            {
                Console.Write("Enter Transaction Id: ");
                var rawId = Console.ReadLine();

                if (!Guid.TryParse(rawId, out var id))
                {
                    Console.WriteLine("Invalid Id format. Please copy the Id from the transaction list.");
                    return;
                }

                await _api.DeleteAsync($"transactions/{id}");

                Console.WriteLine("Transaction deleted.");
            }
            else
            {
                Console.WriteLine("Invalid option.");
            }
        }
    }
}