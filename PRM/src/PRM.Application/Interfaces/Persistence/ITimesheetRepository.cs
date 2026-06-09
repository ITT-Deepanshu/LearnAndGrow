using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface ITimesheetRepository
{
    Task<Timesheet?> GetByEmployeeWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> ListByEmployeeAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> ListForTeamWeekAsync(IReadOnlyList<long> employeeIds, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecentActivityTagsForEmployeeAsync(long employeeId, DateOnly sinceWeekStart, CancellationToken cancellationToken = default);
    void Add(Timesheet timesheet);
}
