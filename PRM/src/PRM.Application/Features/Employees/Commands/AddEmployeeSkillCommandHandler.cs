using MediatR;
using PRM.Application.Features.Employees.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Commands;

public sealed class AddEmployeeSkillCommandHandler(
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<AddEmployeeSkillCommand, EmployeeSkillDto>
{
    public async Task<EmployeeSkillDto> Handle(AddEmployeeSkillCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var employee = await employeeRepository.GetByIdWithSkillsAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        var skill = employee.AddSkill(request.Name, request.Category, request.Proficiency, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_SKILL_ADDED", nameof(Employee), employee.Id, skill.Name, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EmployeeMappings.ToSkillDto(skill);
    }
}
