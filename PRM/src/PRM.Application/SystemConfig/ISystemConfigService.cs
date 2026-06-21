using PRM.Application.SystemConfig;

namespace PRM.Application.SystemConfig;

public interface ISystemConfigService
{
    Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken cancellationToken = default);
    Task<SystemConfigDto> UpdateSystemConfigAsync(UpdateSystemConfigDto dto, CancellationToken cancellationToken = default);
}
