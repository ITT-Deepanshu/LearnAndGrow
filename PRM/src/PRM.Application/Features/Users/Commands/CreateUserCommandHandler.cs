using MediatR;
using PRM.Application.Features.Users.Dtos;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;

namespace PRM.Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByUsernameAsync(username, cancellationToken))
            throw new ConflictException("Username is already taken.");

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;
        var user = UserFactory.CreateAccount(
            username,
            email,
            request.FullName,
            passwordHasher.Hash(request.TemporaryPassword),
            request.Role,
            actorId,
            utcNow);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            userRepository.Add(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.Role is UserRole.Manager or UserRole.Employee)
            {
                var employee = Employee.Create(user.Id, string.Empty, string.Empty, actorId, utcNow);
                employeeRepository.Add(employee);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            auditLogRepository.Add(AuditLog.Create(actorId, "USER_CREATED", nameof(User), user.Id, user.Username, utcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return MapToDto(user);
    }

    private static UserDto MapToDto(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString().ToUpperInvariant(),
            user.IsActive,
            user.ForcePasswordChange);
}
