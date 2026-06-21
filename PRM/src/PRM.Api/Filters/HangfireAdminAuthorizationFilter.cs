using Hangfire.Dashboard;
using PRM.Domain.Constants;

namespace PRM.Api.Filters;

/// <summary>
/// Hangfire dashboard auth. In Development, the dashboard is open (browser has no JWT).
/// Otherwise requires an authenticated user with system.manage permission.
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
            && httpContext.User.HasClaim(PrmClaimTypes.Permission, RolePermissions.SystemManage);
    }
}
