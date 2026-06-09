using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface IAllocationRepository
{
    Task<Allocation?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> ListActiveForEmployeeAsync(long employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> ListActiveOnProjectAsync(long projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allocation>> ListAllAsync(long? employeeId, long? projectId, CancellationToken cancellationToken = default);
    void Add(Allocation allocation);
}
