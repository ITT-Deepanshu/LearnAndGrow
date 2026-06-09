using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Commands;

public sealed class UpdateSkillProficiencyCommandHandler(
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateSkillProficiencyCommand>
{
    public async Task Handle(UpdateSkillProficiencyCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var employee = await employeeRepository.GetByIdWithSkillsAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        employee.UpdateSkillProficiency(request.SkillId, request.Proficiency, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_SKILL_UPDATED", nameof(Employee), employee.Id, request.SkillId.ToString(), utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
