using FinanceTracker.Api.Services;
using FinanceTracker.API.Common;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("budgets")]
    public class BudgetController : ControllerBase
    {
        private readonly BudgetService _service;

        public BudgetController(BudgetService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult Set(BudgetDto dto)
        {
            _service.Set(new Budget { UserId = dto.UserId, Category = dto.Category, Limit = dto.Limit });

            return Ok(new ApiResponse<string> { Success = true, Message = "Budget set" });
        }

        [HttpGet]
        public IActionResult Get(Guid userId)
        {
            return Ok(new ApiResponse<object> { Success = true, Data = _service.GetAll(userId) });
        }
    }
}