using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Users.Commands;
using PRM.Application.Features.Users.Dtos;
using PRM.Application.Features.Users.Queries;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || !Enum.IsDefined(role))
            throw new ValidationException("Role must be Admin, Manager, or Employee.");

        var result = await mediator.Send(new CreateUserCommand(
            dto.FullName,
            dto.Email,
            dto.Username,
            dto.TemporaryPassword,
            role), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListUsersQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetUserPasswordDto dto, CancellationToken cancellationToken)
    {
        await mediator.Send(new ResetUserPasswordCommand(id, dto.NewPassword, dto.ConfirmPassword), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReactivateUserCommand(id), cancellationToken);
        return NoContent();
    }
}
