using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Commands;

public sealed class AssignManagerCommandHandler(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<AssignManagerCommand>
{
    public async Task Handle(AssignManagerCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        var manager = await userRepository.GetByIdAsync(request.ManagerUserId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.Role != UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        employee.AssignManager(request.ManagerUserId, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_MANAGER_ASSIGNED", nameof(Employee), employee.Id, manager.FullName, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
