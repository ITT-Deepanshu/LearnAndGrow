namespace FinanceTracker.ConsoleClient.Services
{
    public class UserSession
    {
        public Guid UserId { get; private set; }

        public void SetUser(Guid userId)
        {
            UserId = userId;
        }
    }
}