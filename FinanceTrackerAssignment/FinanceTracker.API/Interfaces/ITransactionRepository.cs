using FinanceTracker.API.Models;

namespace FinanceTracker.API.Interfaces
{
    public interface ITransactionRepository
    {
        void Add(Transaction t);
        List<Transaction> GetAll();
        void Delete(Guid id);
    }
}
