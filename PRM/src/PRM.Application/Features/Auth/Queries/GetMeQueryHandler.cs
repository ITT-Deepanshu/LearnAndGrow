using MediatR;
using PRM.Application.Features.Auth.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Auth.Queries;

public sealed class GetMeQueryHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser) : IRequestHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return new MeDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString().ToUpperInvariant(),
            user.ForcePasswordChange);
    }
}
