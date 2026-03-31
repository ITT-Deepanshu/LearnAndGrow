using FinanceTracker.API.Common;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult Add(UserDto dto)
        {
            _service.Add(new User { Name = dto.Name });

            return Ok(new ApiResponse<string> { Success = true, Message = "User created" });
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new ApiResponse<object> { Success = true, Data = _service.GetAll() });
        }
    }
}