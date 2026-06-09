using MediatR;

namespace PRM.Application.Features.Users.Commands;

public sealed record DeactivateUserCommand(long UserId) : IRequest;
