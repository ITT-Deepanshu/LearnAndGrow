using Hangfire.Dashboard;

namespace PRM.Api.Filters;

/// <summary>
/// Hangfire dashboard auth. In Development, the dashboard is open (browser has no JWT).
/// Otherwise requires an authenticated admin (role claim is lowercase "admin").
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var environment = httpContext.RequestServices
            .GetService<IWebHostEnvironment>();

        if (environment?.IsDevelopment() == true)
            return true;

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("admin");
    }
}
