using FinanceTracker.API.Common;
using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.Api.Services
{
    public class BudgetService
    {
        private readonly IBudgetRepository _repo;

        public BudgetService(IBudgetRepository repo)
        {
            _repo = repo;
        }

        public void Set(Budget budget)
        {
            if (budget.UserId == Guid.Empty)
                throw new ValidationException("UserId is required");

            if (string.IsNullOrWhiteSpace(budget.Category))
                throw new ValidationException("Category is required");

            if (budget.Limit <= 0)
                throw new ValidationException("Budget limit must be greater than zero");

            _repo.Set(budget);
        }

        public List<Budget> GetAll(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ValidationException("UserId is required");

            return _repo.GetAll()
                        .Where(b => b.UserId == userId)
                        .ToList();
        }
    }
}