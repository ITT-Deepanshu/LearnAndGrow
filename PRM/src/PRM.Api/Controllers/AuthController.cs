using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Auth.Commands;
using PRM.Application.Features.Auth.Dtos;
using PRM.Application.Features.Auth.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new LoginCommand(dto.Username, dto.Password), cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Refresh([FromBody] RefreshDto dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RefreshTokenCommand(dto.RefreshToken), cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutCommand(dto.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new ChangePasswordCommand(dto.CurrentPassword, dto.NewPassword, dto.ConfirmPassword), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetMeQuery(), cancellationToken));
}
