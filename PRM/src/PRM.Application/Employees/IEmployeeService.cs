using PRM.Application.Employees;
using PRM.Domain.Enums;

namespace PRM.Application.Employees;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeListItemDto>> ListEmployeesAsync(
        ResourceProfileStatus? status,
        string? department,
        CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto> GetEmployeeByIdAsync(long resourceProfileId, CancellationToken cancellationToken = default);
    Task UpdateEmployeeAsync(long resourceProfileId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
    Task DeactivateEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default);
    Task ReactivateEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default);
    Task AssignManagerAsync(long resourceProfileId, AssignManagerDto dto, CancellationToken cancellationToken = default);
    Task<ResourceProfileSkillDto> AddSkillAsync(long resourceProfileId, AddResourceProfileSkillDto dto, CancellationToken cancellationToken = default);
    Task UpdateSkillProficiencyAsync(long resourceProfileId, long skillId, UpdateSkillProficiencyDto dto, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(long resourceProfileId, long skillId, CancellationToken cancellationToken = default);
}
