using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Application.Interfaces.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdWithSkillsAsync(long id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> ListAsync(EmployeeStatus? status, string? department, long? managerId, CancellationToken cancellationToken = default);
    void Add(Employee employee);
}
