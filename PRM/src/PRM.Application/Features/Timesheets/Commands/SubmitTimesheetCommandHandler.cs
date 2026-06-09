using MediatR;
using PRM.Application.Features.Timesheets.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Timesheets.Commands;

public sealed class SubmitTimesheetCommandHandler(
    ITimesheetRepository timesheetRepository,
    IAllocationRepository allocationRepository,
    IActivityTagRepository activityTagRepository,
    ISystemConfigRepository systemConfigRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<SubmitTimesheetCommand, TimesheetDto>
{
    public async Task<TimesheetDto> Handle(SubmitTimesheetCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Employee)
            throw new ForbiddenException("Employee role required.");

        var employee = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Employee profile not found.");

        if (employee.Status == EmployeeStatus.Inactive || !employee.User.IsActive)
            throw new BusinessRuleException("Employee is not active.");

        if (!WeekHelper.IsMonday(request.WeekStart))
            throw new ValidationException("Week start must be a Monday.");

        if (WeekHelper.IsFutureWeek(request.WeekStart, clock.Today))
            throw new BusinessRuleException("Week start cannot be in the future.");

        var existing = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, request.WeekStart, cancellationToken);
        if (existing is not null)
            throw new ConflictException("A timesheet for this week has already been submitted.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var maxWeeklyHours = config.MaxWeeklyHours;

        var weekEnd = request.WeekStart.AddDays(6);
        var allocations = await allocationRepository.ListActiveForEmployeeAsync(
            employee.Id,
            request.WeekStart,
            weekEnd,
            cancellationToken);

        var allocationByProject = allocations
            .GroupBy(a => a.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.UtilisationPercentage));

        var allTags = await activityTagRepository.ListAllAsync(cancellationToken);
        var tagById = allTags.ToDictionary(t => t.Id);

        var entries = new List<TimesheetEntry>();
        foreach (var entryCommand in request.Entries)
        {
            if (!allocationByProject.TryGetValue(entryCommand.ProjectId, out var utilisation))
                throw new BusinessRuleException($"Project {entryCommand.ProjectId} is not allocated for this week.");

            var maxHoursForProject = utilisation / 100m * maxWeeklyHours;
            var tags = ResolveTags(entryCommand, tagById);

            entries.Add(TimesheetEntry.Create(
                entryCommand.ProjectId,
                entryCommand.Hours,
                tags,
                maxHoursForProject,
                currentUser.UserId.Value,
                clock.UtcNow));
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var timesheet = TimesheetFactory.Submit(
                employee.Id,
                request.WeekStart,
                entries,
                maxWeeklyHours,
                currentUser.UserId.Value,
                clock.UtcNow);

            timesheetRepository.Add(timesheet);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var saved = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, request.WeekStart, cancellationToken)
                ?? throw new NotFoundException("Timesheet not found after submission.");

            return TimesheetMappings.ToDto(saved);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static IReadOnlyList<ActivityTag> ResolveTags(
        SubmitTimesheetEntryCommand entryCommand,
        IReadOnlyDictionary<int, ActivityTag> tagById)
    {
        var tags = new List<ActivityTag>();
        foreach (var tagId in entryCommand.TagIds.Distinct())
        {
            if (!tagById.TryGetValue(tagId, out var tag))
                throw new ValidationException($"Activity tag {tagId} was not found.");

            tags.Add(tag);
        }

        if (tags.Any(t => t.IsCustom) && string.IsNullOrWhiteSpace(entryCommand.CustomText))
            throw new ValidationException("Custom text is required when the Other activity tag is selected.");

        return tags;
    }
}
