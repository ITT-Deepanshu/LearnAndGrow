using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly List<User> _data = new();

        public void Add(User user) => _data.Add(user);

        public List<User> GetAll() => _data;
    }
}
