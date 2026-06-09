using MediatR;
using PRM.Application.Features.Users.Dtos;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Users.Queries;

public sealed class GetUserByIdQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString().ToUpperInvariant(),
            user.IsActive,
            user.ForcePasswordChange);
    }
}
