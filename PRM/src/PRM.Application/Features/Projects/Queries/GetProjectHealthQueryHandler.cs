using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Application.Features.Projects.Queries;

public sealed class GetProjectHealthQueryHandler(
    IProjectRepository projectRepository,
    ISystemConfigRepository systemConfigRepository,
    ITimesheetRepository timesheetRepository,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetProjectHealthQuery, ProjectHealthDto>
{
    private readonly ProjectHealthDomainService _healthService = new();

    public async Task<ProjectHealthDto> Handle(GetProjectHealthQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        EnsureCanViewProject(project.ManagerId);

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var today = clock.Today;
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var effortData = new List<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)>();

        foreach (var allocation in project.Allocations.Where(a => a.IsActiveOn(today)))
        {
            var expectedHours = allocation.UtilisationPercentage * config.MaxWeeklyHours / 100m;
            var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(
                allocation.EmployeeId,
                lastWeekStart,
                cancellationToken);

            var loggedHours = timesheet?.Entries
                .Where(e => e.ProjectId == project.Id)
                .Sum(e => e.Hours) ?? 0m;

            effortData.Add((allocation.Employee.User.FullName, expectedHours, loggedHours));
        }

        var result = _healthService.Evaluate(project, today, effortData);

        return new ProjectHealthDto(
            project.Id,
            result.Status.ToString(),
            result.DisplayLabel,
            result.RiskFlags);
    }

    private void EnsureCanViewProject(long managerId)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        if (currentUser.Role == UserRole.Manager && currentUser.UserId == managerId)
            return;

        throw new ForbiddenException("You do not have access to this project.");
    }
}
