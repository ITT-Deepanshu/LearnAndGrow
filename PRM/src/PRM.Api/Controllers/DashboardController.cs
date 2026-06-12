using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Dashboard;

namespace PRM.Api.Controllers;

/// <summary>Manager dashboards and resource utilisation summaries.</summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
[Tags("Dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>Resource utilisation dashboard: bench, partially allocated, fully allocated, and drill-down. Manager only.</summary>
    [HttpGet("resources")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> GetResources(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetResourceDashboardAsync(cancellationToken));
}
