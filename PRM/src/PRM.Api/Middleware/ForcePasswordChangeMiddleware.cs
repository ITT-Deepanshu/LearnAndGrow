namespace PRM.Api.Middleware;

public sealed class ForcePasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/change-password",
        "/api/v1/auth/logout",
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/health/live",
        "/health/ready",
        "/swagger",
        "/swagger/index.html"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var forceChange = context.User.FindFirst("force_password_change")?.Value == "true";
        var path = context.Request.Path.Value ?? string.Empty;

        if (forceChange && context.User.Identity?.IsAuthenticated == true
            && !AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { detail = "Password change required before accessing this resource." });
            return;
        }

        await next(context);
    }
}
