using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PRM.Application.Interfaces.Persistence;

namespace PRM.Api.Middleware;

public sealed class RequiresPasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/change-password",
        "/api/v1/auth/me",
        "/api/v1/auth/logout",
        "/api/v1/auth/login",
        "/health/live",
        "/health/ready",
        "/swagger",
        "/swagger/index.html",
        "/hangfire"
    };

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (context.User.Identity?.IsAuthenticated == true
            && !AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (long.TryParse(userIdValue, out var userId))
            {
                var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);
                if (user?.RequiresPasswordChange == true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        detail = "Password change required before accessing this resource."
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}
