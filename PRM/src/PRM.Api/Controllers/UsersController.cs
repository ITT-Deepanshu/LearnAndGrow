using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Api.Authorization;
using PRM.Application.Users;
using PRM.Domain.Constants;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Api.Controllers;

/// <summary>User account lifecycle: create, list, reset password, activate/deactivate. Admin only.</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
[RequirePermission(RolePermissions.UsersManage)]
[Tags("Users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Create a new user with role (admin, manager, or resource) and a temporary password.</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || !Enum.IsDefined(role))
            throw new ValidationException("Role must be Admin, Manager, or Resource.");

        var result = await userService.CreateUserAsync(dto, role, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>List all users with role and active status.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await userService.ListUsersAsync(cancellationToken));

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await userService.GetUserByIdAsync(id, cancellationToken));

    /// <summary>Reset a user's password to a new temporary value (forces change on next login).</summary>
    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetUserPasswordDto dto, CancellationToken cancellationToken)
    {
        await userService.ResetUserPasswordAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>Deactivate a user account (blocks login).</summary>
    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        await userService.DeactivateUserAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Reactivate a previously deactivated user account.</summary>
    [HttpPost("{id:long}/reactivate")]
    public async Task<IActionResult> Reactivate(long id, CancellationToken cancellationToken)
    {
        await userService.ReactivateUserAsync(id, cancellationToken);
        return NoContent();
    }
}
