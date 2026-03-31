using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class MainMenu
    {
        private readonly UserSession _session;
        private readonly UserScreen _userScreen;
        private readonly TransactionScreen _transaction;
        private readonly BudgetScreen _budget;
        private readonly ReportScreen _report;

        public MainMenu(ApiService api)
        {
            _session = new UserSession();
            _userScreen = new UserScreen(api);
            _transaction = new TransactionScreen(api, _session);
            _budget = new BudgetScreen(api, _session);
            _report = new ReportScreen(api, _session);
        }

        public async Task Start()
        {
            var userId = await _userScreen.SelectOrCreateUser();
            _session.SetUser(userId);

            while (true)
            {
                Console.WriteLine("\n1. Transactions\n2. Budget\n3. Reports\n4. Exit");

                var input = Console.ReadLine();

                try
                {
                    switch (input)
                    {
                        case "1": await _transaction.Show(); break;
                        case "2": await _budget.Show(); break;
                        case "3": await _report.Show(); break;
                        case "4": return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
