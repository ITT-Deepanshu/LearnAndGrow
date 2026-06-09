using MediatR;
using PRM.Application.Features.Timesheets.Dtos;

namespace PRM.Application.Features.Timesheets.Queries;

public sealed record GetMyTimesheetForWeekQuery(DateOnly WeekStart) : IRequest<TimesheetDto?>;
