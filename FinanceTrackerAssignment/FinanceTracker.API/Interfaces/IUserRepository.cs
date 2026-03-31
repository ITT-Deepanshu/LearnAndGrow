using FinanceTracker.API.Models;

namespace FinanceTracker.API.Interfaces
{
    public interface IUserRepository
    {
        void Add(User user);
        List<User> GetAll();
    }
}
