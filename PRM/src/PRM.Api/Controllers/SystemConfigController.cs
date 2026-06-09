using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.Features.SystemConfig.Commands;
using PRM.Application.Features.SystemConfig.Dtos;
using PRM.Application.Features.SystemConfig.Queries;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/v1/system-config")]
[Authorize(Roles = "Admin")]
public sealed class SystemConfigController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SystemConfigDto>> Get(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetSystemConfigQuery(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<SystemConfigDto>> Update(
        [FromBody] UpdateSystemConfigDto dto,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(dto.LlmApiKey) && !IsMaskedApiKey(dto.LlmApiKey))
            await mediator.Send(new UpdateLlmApiKeyCommand(dto.LlmApiKey), cancellationToken);

        await mediator.Send(new UpdateLlmProviderCommand(dto.LlmProvider), cancellationToken);
        await mediator.Send(new UpdateSchedulerIntervalCommand(dto.SchedulerIntervalMinutes), cancellationToken);
        await mediator.Send(new UpdateMaxWeeklyHoursCommand(dto.MaxWeeklyHours), cancellationToken);

        return Ok(await mediator.Send(new GetSystemConfigQuery(), cancellationToken));
    }

    private static bool IsMaskedApiKey(string apiKey) =>
        apiKey.All(c => c == '*');
}
