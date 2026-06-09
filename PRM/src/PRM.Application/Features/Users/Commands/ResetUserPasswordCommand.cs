using MediatR;

namespace PRM.Application.Features.Users.Commands;

public sealed record ResetUserPasswordCommand(long UserId, string NewPassword, string ConfirmPassword) : IRequest;
