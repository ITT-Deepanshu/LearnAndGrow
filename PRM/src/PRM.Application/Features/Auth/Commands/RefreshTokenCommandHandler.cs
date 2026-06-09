using MediatR;
using PRM.Application.Features.Auth.Dtos;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<RefreshTokenCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (stored.IsRevoked)
        {
            refreshTokenRepository.RevokeAllForUser(stored.UserId, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse detected.");
        }

        if (stored.IsExpired(clock.UtcNow))
            throw new UnauthorizedException("Refresh token expired.");

        var user = await userRepository.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is disabled.");

        stored.Revoke(clock.UtcNow);
        var newRefreshPlain = tokenService.GenerateRefreshToken();
        refreshTokenRepository.Add(RefreshToken.Create(user.Id, tokenService.HashToken(newRefreshPlain), clock.UtcNow.AddDays(7), clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResultDto(
            tokenService.GenerateAccessToken(user),
            newRefreshPlain,
            user.ForcePasswordChange,
            user.Role.ToString().ToUpperInvariant(),
            user.FullName);
    }
}
