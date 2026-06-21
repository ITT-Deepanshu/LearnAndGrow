using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Auth;

namespace PRM.Api.Controllers;

/// <summary>Login, logout, password change, and current-user profile.</summary>
[ApiController]
[Route("api/v1/auth")]
[Tags("Auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Authenticate with username and password. Returns a JWT access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        if (result.IsFailure)
        {
            var statusCode = result.Error == AuthErrors.AccountDisabled
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;
            var title = statusCode == StatusCodes.Status403Forbidden ? "Forbidden" : "Unauthorized";
            return Problem(statusCode: statusCode, title: title, detail: result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>Invalidate the current session (client should discard the token).</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Change the logged-in user's password. Required on first login when temporary password was issued.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<LoginResultDto>> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken) =>
        Ok(await authService.ChangePasswordAsync(dto, cancellationToken));

    /// <summary>Get the current user's profile, role, and resource profile ID.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetMeAsync(cancellationToken));
}
