using MediatR;
using PRM.Application.Features.Users.Dtos;
using PRM.Application.Interfaces.Persistence;

namespace PRM.Application.Features.Users.Queries;

public sealed class ListUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserListItemDto>>
{
    public async Task<IReadOnlyList<UserListItemDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return users
            .Select(u => new UserListItemDto(
                u.Id,
                u.Username,
                u.Role.ToString().ToUpperInvariant(),
                u.IsActive))
            .ToList();
    }
}
