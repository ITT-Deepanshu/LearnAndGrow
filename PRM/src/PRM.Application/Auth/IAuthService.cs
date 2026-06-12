using PRM.Application.Auth;
using PRM.SharedKernel;

namespace PRM.Application.Auth;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<LoginResultDto> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default);
}
