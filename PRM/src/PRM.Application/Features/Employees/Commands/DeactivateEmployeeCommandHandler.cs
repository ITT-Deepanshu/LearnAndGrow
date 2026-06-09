using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Commands;

public sealed class DeactivateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<DeactivateEmployeeCommand>
{
    public async Task Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        if (employee.Status == EmployeeStatus.Inactive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;
        var today = clock.Today;

        foreach (var allocation in employee.Allocations.Where(a => a.EndedAt is null))
            allocation.End(today, actorId, utcNow);

        employee.Deactivate(actorId, utcNow);

        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new NotFoundException("Linked user not found.");

        user.Deactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_DEACTIVATED", nameof(Employee), employee.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
