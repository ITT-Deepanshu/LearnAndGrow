using FinanceTracker.API.Common;
using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly List<Transaction> _data = new();

        public void Add(Transaction t) => _data.Add(t);

        public List<Transaction> GetAll() => _data;

        public void Delete(Guid id)
        {
            var txn = _data.FirstOrDefault(x => x.Id == id);
            if (txn == null)
                throw new NotFoundException("Transaction not found");

            _data.Remove(txn);
        }
    }
}
