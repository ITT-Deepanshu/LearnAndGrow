using MediatR;
using PRM.Application.Features.Auth.Dtos;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Auth.Commands;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is disabled.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            auditLogRepository.Add(AuditLog.Create(null, "LOGIN_FAILED", nameof(User), user.Id, user.Username, clock.UtcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials.");
        }

        user.RecordLogin(clock.UtcNow);
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenPlain = tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, tokenService.HashToken(refreshTokenPlain), clock.UtcNow.AddDays(7), clock.UtcNow);
        refreshTokenRepository.Add(refreshToken);
        auditLogRepository.Add(AuditLog.Create(user.Id, "LOGIN_SUCCESS", nameof(User), user.Id, null, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResultDto(
            accessToken,
            refreshTokenPlain,
            user.ForcePasswordChange,
            user.Role.ToString().ToUpperInvariant(),
            user.FullName);
    }
}
