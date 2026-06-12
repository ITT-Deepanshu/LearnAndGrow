using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.SystemConfig;

namespace PRM.Api.Controllers;

/// <summary>Global system settings: Gemma API key, scheduler interval, and max weekly hours. Admin only.</summary>
[ApiController]
[Route("api/v1/system-config")]
[Authorize(Roles = "admin")]
[Tags("System Config")]
public sealed class SystemConfigController(ISystemConfigService systemConfigService) : ControllerBase
{
    /// <summary>Get current system configuration (API key is masked).</summary>
    [HttpGet]
    public async Task<ActionResult<SystemConfigDto>> Get(CancellationToken cancellationToken) =>
        Ok(await systemConfigService.GetSystemConfigAsync(cancellationToken));

    /// <summary>Update Gemma API key, background scheduler interval, or max weekly hours.</summary>
    [HttpPut]
    public async Task<ActionResult<SystemConfigDto>> Update(
        [FromBody] UpdateSystemConfigDto dto,
        CancellationToken cancellationToken) =>
        Ok(await systemConfigService.UpdateSystemConfigAsync(dto, cancellationToken));
}
