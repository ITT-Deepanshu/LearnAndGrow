using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.Dashboard.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("resources")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetResources(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetResourceDashboardQuery(), cancellationToken));
}
