using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Repositories
{
    public class BudgetRepository : IBudgetRepository
    {
        private readonly List<Budget> _data = new();

        public void Set(Budget b)
        {
            var existing = _data.FirstOrDefault(x => x.UserId == b.UserId && x.Category == b.Category);
            if (existing != null) _data.Remove(existing);

            _data.Add(b);
        }

        public List<Budget> GetAll() => _data;
    }
}
