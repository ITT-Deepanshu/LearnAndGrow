using FinanceTracker.API.Common;
using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public class TransactionService
    {
        private readonly ITransactionRepository _repo;
        private readonly IBudgetRepository _budgetRepo;
        private readonly INotificationService _notification;

        public TransactionService(ITransactionRepository r, IBudgetRepository b, INotificationService n)
        {
            _repo = r;
            _budgetRepo = b;
            _notification = n;
        }

        public void Add(Transaction t)
        {
            if (t.Amount <= 0)
                throw new ValidationException("Amount must be greater than zero");

            _repo.Add(t);

            if (t.Type == TransactionType.Expense)
            {
                var total = _repo.GetAll()
                .Where(x => x.UserId == t.UserId &&
                            x.Category == t.Category &&
                            x.Type == TransactionType.Expense)
                .Sum(x => x.Amount);

                var budget = _budgetRepo.GetAll()
                    .FirstOrDefault(x => x.Category == t.Category);

                if (budget != null && total > budget.Limit)
                {
                    _notification.Send($"Budget exceeded for {t.Category}");
                }
            }
        }

        public List<Transaction> GetAll(Guid userId, string category = null)
        {
            var data = _repo.GetAll().Where(x => x.UserId == userId).ToList();

            if (!string.IsNullOrEmpty(category))
                data = data.Where(x => x.Category == category).ToList();

            return data;
        }

        public void Delete(Guid id) => _repo.Delete(id);
    }
}