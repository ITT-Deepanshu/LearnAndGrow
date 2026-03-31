using FinanceTracker.API.Common;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _service;

        public ReportController(ReportService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public IActionResult Summary(Guid userId)
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = _service.GetSummary(userId)
            });
        }
    }
}