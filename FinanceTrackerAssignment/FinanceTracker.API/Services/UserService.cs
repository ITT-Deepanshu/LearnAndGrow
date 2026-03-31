using FinanceTracker.API.Common;
using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public void Add(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
                throw new ValidationException("Name is required");

            _repo.Add(user);
        }

        public List<User> GetAll() => _repo.GetAll();
    }
}