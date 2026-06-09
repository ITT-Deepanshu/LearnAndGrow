using MediatR;

namespace PRM.Application.Features.Users.Commands;

public sealed record ReactivateUserCommand(long UserId) : IRequest;
