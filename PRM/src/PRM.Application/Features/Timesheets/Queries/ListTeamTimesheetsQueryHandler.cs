using MediatR;
using PRM.Application.Features.Timesheets.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Timesheets.Queries;

public sealed class ListTeamTimesheetsQueryHandler(
    ITimesheetRepository timesheetRepository,
    IProjectRepository projectRepository,
    IAllocationRepository allocationRepository,
    ICurrentUser currentUser) : IRequestHandler<ListTeamTimesheetsQuery, IReadOnlyList<TeamTimesheetRowDto>>
{
    public async Task<IReadOnlyList<TeamTimesheetRowDto>> Handle(ListTeamTimesheetsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        if (!WeekHelper.IsMonday(request.WeekStart))
            throw new ValidationException("Week start must be a Monday.");

        var projects = await projectRepository.ListAsync(currentUser.UserId, cancellationToken);
        if (projects.Count == 0)
            return [];

        var projectIds = projects.Select(p => p.Id).ToHashSet();
        var weekEnd = request.WeekStart.AddDays(6);
        var employeeIds = new HashSet<long>();

        foreach (var project in projects)
        {
            var allocations = await allocationRepository.ListActiveOnProjectAsync(project.Id, cancellationToken);
            foreach (var allocation in allocations.Where(a => a.FromDate <= weekEnd && a.ToDate >= request.WeekStart))
                employeeIds.Add(allocation.EmployeeId);
        }

        if (employeeIds.Count == 0)
            return [];

        var timesheets = await timesheetRepository.ListForTeamWeekAsync(
            employeeIds.ToList(),
            request.WeekStart,
            cancellationToken);

        var rows = new List<TeamTimesheetRowDto>();
        foreach (var timesheet in timesheets)
        {
            var employeeName = timesheet.Employee?.User?.FullName ?? string.Empty;
            var status = timesheet.Status.ToString();

            foreach (var entry in timesheet.Entries.Where(e => projectIds.Contains(e.ProjectId)))
            {
                rows.Add(new TeamTimesheetRowDto(
                    employeeName,
                    entry.Project?.Name ?? string.Empty,
                    entry.Hours,
                    status));
            }
        }

        return rows
            .OrderBy(r => r.EmployeeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
