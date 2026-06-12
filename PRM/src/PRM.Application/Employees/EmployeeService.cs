using PRM.Application.Common;
using PRM.Application.Employees;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Employees;

public sealed class EmployeeService(
    IResourceProfileRepository employeeRepository,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> ListEmployeesAsync(
        ResourceProfileStatus? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        long? managerFilter = currentUser.Role switch
        {
            UserRole.Admin => null,
            UserRole.Manager => currentUser.UserId,
            _ => throw new ForbiddenException("Insufficient permissions to list employees.")
        };

        var employees = await employeeRepository.ListAsync(status, department, managerFilter, cancellationToken);
        return employees.Select(EmployeeMappings.ToListItemDto).ToList();
    }

    public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(long resourceProfileId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var resourceProfile = await employeeRepository.GetByIdWithSkillsAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        CurrentUserGuards.EnsureCanViewEmployee(currentUser, resourceProfile.ManagerId, resourceProfile.Status);

        return EmployeeMappings.ToDetailDto(resourceProfile);
    }

    public async Task UpdateEmployeeAsync(long resourceProfileId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        resourceProfile.Update(resourceProfile.FullName, dto.Department, dto.Designation, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_UPDATED", nameof(ResourceProfile), resourceProfile.Id, null, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (resourceProfile.Status == ResourceProfileStatus.Inactive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;
        var today = clock.Today;

        foreach (var allocation in resourceProfile.Allocations.Where(a => a.EndedAt is null))
            allocation.End(today, actorId, utcNow);

        resourceProfile.Deactivate(actorId, utcNow);

        var user = await userRepository.GetByIdAsync(resourceProfile.UserId, cancellationToken)
            ?? throw new NotFoundException("Linked user not found.");

        user.Deactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_DEACTIVATED", nameof(ResourceProfile), resourceProfile.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (resourceProfile.Status != ResourceProfileStatus.Inactive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        resourceProfile.Reactivate(actorId, utcNow);

        var user = await userRepository.GetByIdAsync(resourceProfile.UserId, cancellationToken)
            ?? throw new NotFoundException("Linked user not found.");

        user.Reactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_REACTIVATED", nameof(ResourceProfile), resourceProfile.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignManagerAsync(long resourceProfileId, AssignManagerDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var manager = await userRepository.GetByIdAsync(dto.ManagerId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.RoleId != (long)UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        resourceProfile.AssignManager(dto.ManagerId, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_MANAGER_ASSIGNED", nameof(ResourceProfile), resourceProfile.Id, manager.ResourceProfile?.FullName ?? manager.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ResourceProfileSkillDto> AddSkillAsync(long resourceProfileId, AddResourceProfileSkillDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdWithSkillsAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        var skill = resourceProfile.AddSkill(dto.Name, dto.Category, dto.Proficiency, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_SKILL_ADDED", nameof(ResourceProfile), resourceProfile.Id, skill.Name, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EmployeeMappings.ToSkillDto(skill);
    }

    public async Task UpdateSkillProficiencyAsync(long resourceProfileId, long skillId, UpdateSkillProficiencyDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdWithSkillsAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        resourceProfile.UpdateSkillProficiency(skillId, dto.Proficiency, actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_SKILL_UPDATED", nameof(ResourceProfile), resourceProfile.Id, skillId.ToString(), utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var resourceProfile = await employeeRepository.GetByIdWithSkillsAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        resourceProfile.RemoveSkill(skillId);

        auditLogRepository.Add(AuditLog.Create(actorId, "EMPLOYEE_SKILL_REMOVED", nameof(ResourceProfile), resourceProfile.Id, skillId.ToString(), utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
