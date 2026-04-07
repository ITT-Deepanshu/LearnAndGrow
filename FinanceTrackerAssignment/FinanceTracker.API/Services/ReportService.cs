using FinanceTracker.API.Interfaces;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public class ReportService
    {
        private readonly ITransactionRepository _repo;

        public ReportService(ITransactionRepository repo)
        {
            _repo = repo;
        }

        public object GetSummary(Guid userId)
        {
            var data = _repo.GetAll().Where(x => x.UserId == userId);

            var income = data.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
            var expense = data.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

            return new { income, expense, savings = income - expense };
        }
    }
}