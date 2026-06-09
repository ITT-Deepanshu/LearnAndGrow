using MediatR;
using PRM.Application.Features.Timesheets.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Timesheets.Queries;

public sealed class ListMyTimesheetsQueryHandler(
    ITimesheetRepository timesheetRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IRequestHandler<ListMyTimesheetsQuery, IReadOnlyList<TimesheetListItemDto>>
{
    public async Task<IReadOnlyList<TimesheetListItemDto>> Handle(ListMyTimesheetsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Employee)
            throw new ForbiddenException("Employee role required.");

        var employee = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Employee profile not found.");

        var timesheets = await timesheetRepository.ListByEmployeeAsync(employee.Id, cancellationToken);
        return timesheets.Select(TimesheetMappings.ToListItemDto).ToList();
    }
}
