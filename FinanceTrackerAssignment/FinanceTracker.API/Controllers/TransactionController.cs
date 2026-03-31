using FinanceTracker.API.Common;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _service;

        public TransactionController(TransactionService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult Add(TransactionRequestDto dto)
        {
            var txn = new Transaction
            {
                UserId = dto.UserId,
                Type = Enum.Parse<TransactionType>(dto.Type, true),
                Amount = dto.Amount,
                Category = dto.Category,
                Date = DateTime.Now
            };

            _service.Add(txn);

            return Ok(new ApiResponse<string> { Success = true, Message = "Transaction added" });
        }

        [HttpGet]
        public IActionResult Get(Guid userId, string? category)
        {
            var data = _service.GetAll(userId, category);
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _service.Delete(id);
            return Ok(new ApiResponse<string> { Success = true, Message = "Deleted" });
        }
    }
}