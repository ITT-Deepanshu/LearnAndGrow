using MediatR;
using PRM.Application.Features.Timesheets.Dtos;

namespace PRM.Application.Features.Timesheets.Commands;

public sealed record SubmitTimesheetEntryCommand(
    long ProjectId,
    decimal Hours,
    IReadOnlyList<int> TagIds,
    string? CustomText);

public sealed record SubmitTimesheetCommand(
    DateOnly WeekStart,
    IReadOnlyList<SubmitTimesheetEntryCommand> Entries) : IRequest<TimesheetDto>;
