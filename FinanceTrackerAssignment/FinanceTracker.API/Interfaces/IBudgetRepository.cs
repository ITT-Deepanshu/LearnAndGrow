using FinanceTracker.API.Models;

namespace FinanceTracker.API.Interfaces
{
    public interface IBudgetRepository
    {
        void Set(Budget b);
        List<Budget> GetAll();
    }
}
