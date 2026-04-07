using FinanceTracker.API.Interfaces;

namespace FinanceTracker.API.Adapter
{
    public class ConsoleNotificationService : INotificationService
    {
        public void Send(string message)
        {
            Console.WriteLine($"[ALERT]: {message}");
        }
    }
}
