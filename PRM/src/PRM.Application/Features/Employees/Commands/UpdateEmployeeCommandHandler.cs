using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Commands;

public sealed class UpdateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateEmployeeCommand>
{
    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        employee.Update(request.Department, request.Designation, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_UPDATED", nameof(Employee), employee.Id, null, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
