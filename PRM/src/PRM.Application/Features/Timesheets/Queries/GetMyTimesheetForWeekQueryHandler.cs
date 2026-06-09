using MediatR;
using PRM.Application.Features.Timesheets.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Timesheets.Queries;

public sealed class GetMyTimesheetForWeekQueryHandler(
    ITimesheetRepository timesheetRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IRequestHandler<GetMyTimesheetForWeekQuery, TimesheetDto?>
{
    public async Task<TimesheetDto?> Handle(GetMyTimesheetForWeekQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Employee)
            throw new ForbiddenException("Employee role required.");

        if (!WeekHelper.IsMonday(request.WeekStart))
            throw new ValidationException("Week start must be a Monday.");

        var employee = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Employee profile not found.");

        var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, request.WeekStart, cancellationToken);
        return timesheet is null ? null : TimesheetMappings.ToDto(timesheet);
    }
}
