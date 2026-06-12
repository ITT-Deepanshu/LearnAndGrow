using PRM.Application.Timesheets;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Timesheets;

public sealed class TimesheetService(
    ITimesheetRepository timesheetRepository,
    IAllocationRepository allocationRepository,
    IActivityTagRepository activityTagRepository,
    ISystemConfigRepository systemConfigRepository,
    IResourceProfileRepository employeeRepository,
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ITimesheetService
{
    public async Task<TimesheetDto> SubmitTimesheetAsync(SubmitTimesheetDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        var resourceProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (resourceProfile.Status == ResourceProfileStatus.Inactive || !resourceProfile.User.IsActive)
            throw new BusinessRuleException("Resource profile is not active.");

        if (!WeekHelper.IsMonday(dto.WeekStart))
            throw new ValidationException("Week start must be a Monday.");

        if (WeekHelper.IsFutureWeek(dto.WeekStart, clock.Today))
            throw new BusinessRuleException("Week start cannot be in the future.");

        var existing = await timesheetRepository.GetByEmployeeWeekAsync(resourceProfile.Id, dto.WeekStart, cancellationToken);
        if (existing is not null && existing.Status == TimesheetStatus.Submitted)
            throw new ConflictException("A timesheet for this week has already been submitted.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var maxWeeklyHours = config.MaxWeeklyHours;

        var weekEnd = dto.WeekStart.AddDays(6);
        var allocations = await allocationRepository.ListActiveForEmployeeAsync(
            resourceProfile.Id,
            dto.WeekStart,
            weekEnd,
            cancellationToken);

        var allocationByProject = allocations
            .GroupBy(a => a.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.UtilisationPercentage));

        var allTags = await activityTagRepository.ListAllAsync(cancellationToken);
        var tagById = allTags.ToDictionary(t => t.Id);

        var entries = new List<TimesheetEntry>();
        foreach (var entryDto in dto.Entries)
        {
            if (!allocationByProject.TryGetValue(entryDto.ProjectId, out var utilisation))
                throw new BusinessRuleException($"Project {entryDto.ProjectId} is not allocated for this week.");

            var maxHoursForProject = utilisation / 100m * maxWeeklyHours;
            var tags = ResolveTags(entryDto, tagById);

            entries.Add(TimesheetEntry.Create(
                entryDto.ProjectId,
                entryDto.Hours,
                tags,
                maxHoursForProject,
                currentUser.UserId.Value,
                clock.UtcNow));
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (existing is not null && existing.Status == TimesheetStatus.Missed)
                timesheetRepository.Remove(existing);

            var timesheet = Timesheet.Submit(
                resourceProfile.Id,
                dto.WeekStart,
                entries,
                maxWeeklyHours,
                currentUser.UserId.Value,
                clock.UtcNow);

            timesheetRepository.Add(timesheet);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var saved = await timesheetRepository.GetByEmployeeWeekAsync(resourceProfile.Id, dto.WeekStart, cancellationToken)
                ?? throw new NotFoundException("Timesheet not found after submission.");

            return TimesheetMappings.ToDto(saved);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimesheetListItemDto>> ListMyTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        var resourceProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var timesheets = await timesheetRepository.ListByEmployeeAsync(resourceProfile.Id, cancellationToken);
        return timesheets.Select(TimesheetMappings.ToListItemDto).ToList();
    }

    public async Task<TimesheetDto?> GetMyTimesheetForWeekAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        if (!WeekHelper.IsMonday(weekStart))
            throw new ValidationException("Week start must be a Monday.");

        var resourceProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(resourceProfile.Id, weekStart, cancellationToken);
        return timesheet is null ? null : TimesheetMappings.ToDto(timesheet);
    }

    public async Task<IReadOnlyList<TeamTimesheetRowDto>> ListTeamTimesheetsAsync(DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        if (!WeekHelper.IsMonday(weekStart))
            throw new ValidationException("Week start must be a Monday.");

        var projects = await projectRepository.ListAsync(currentUser.UserId, cancellationToken);
        if (projects.Count == 0)
            return [];

        var projectIds = projects.Select(p => p.Id).ToHashSet();
        var weekEnd = weekStart.AddDays(6);
        var activeAllocations = new List<Allocation>();

        foreach (var project in projects)
        {
            var allocations = await allocationRepository.ListActiveOnProjectAsync(project.Id, cancellationToken);
            activeAllocations.AddRange(allocations.Where(a => a.FromDate <= weekEnd && a.ToDate >= weekStart));
        }

        if (activeAllocations.Count == 0)
            return [];

        var employeeIds = activeAllocations.Select(a => a.ResourceProfileId).Distinct().ToList();
        var timesheets = await timesheetRepository.ListForTeamWeekAsync(employeeIds, weekStart, cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(t => t.ResourceProfileId);
        var weekCompleted = weekEnd < clock.Today;

        var rows = new List<TeamTimesheetRowDto>();
        foreach (var allocation in activeAllocations.OrderBy(a => a.ResourceProfile.FullName).ThenBy(a => a.Project.Name))
        {
            var employeeName = allocation.ResourceProfile?.FullName ?? string.Empty;
            var projectName = allocation.Project?.Name ?? string.Empty;
            timesheetByEmployee.TryGetValue(allocation.ResourceProfileId, out var timesheet);

            if (timesheet?.Status == TimesheetStatus.Submitted)
            {
                var entry = timesheet.Entries.FirstOrDefault(e => e.ProjectId == allocation.ProjectId);
                if (entry is null)
                    continue;

                rows.Add(new TeamTimesheetRowDto(
                    employeeName,
                    projectName,
                    entry.Hours,
                    TimesheetStatus.Submitted.ToString()));
                continue;
            }

            var status = timesheet?.Status == TimesheetStatus.Missed || weekCompleted
                ? TimesheetStatus.Missed.ToString()
                : "NOT_SUBMITTED";

            rows.Add(new TeamTimesheetRowDto(employeeName, projectName, 0, status));
        }

        return rows;
    }

    public async Task<TimesheetSubmissionContextDto> GetSubmissionContextAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var tags = await activityTagRepository.ListAllAsync(cancellationToken);

        return new TimesheetSubmissionContextDto(
            config.MaxWeeklyHours,
            tags.Select(t => new ActivityTagDto(t.Id, t.Name, t.IsCustom)).ToList());
    }

    public async Task<TimesheetReminderDto?> GetMissedTimesheetReminderAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        var resourceProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (resourceProfile.Status == ResourceProfileStatus.Inactive || !resourceProfile.User.IsActive)
            return null;

        var weekStart = WeekHelper.GetLastCompletedWeekMonday(clock.Today);
        var weekEnd = weekStart.AddDays(6);

        var existing = await timesheetRepository.GetByEmployeeWeekAsync(resourceProfile.Id, weekStart, cancellationToken);
        if (existing?.Status == TimesheetStatus.Submitted)
            return null;

        var allocations = await allocationRepository.ListActiveForEmployeeAsync(
            resourceProfile.Id,
            weekStart,
            weekEnd,
            cancellationToken);

        if (allocations.Count == 0)
            return null;

        return new TimesheetReminderDto(
            weekStart,
            $"Reminder: Timesheet for week {weekStart:dd-MM-yyyy} has not been submitted.");
    }

    private static IReadOnlyList<ActivityTag> ResolveTags(
        SubmitTimesheetEntryDto entryDto,
        IReadOnlyDictionary<int, ActivityTag> tagById)
    {
        var tags = new List<ActivityTag>();
        foreach (var tagId in entryDto.TagIds.Distinct())
        {
            if (!tagById.TryGetValue(tagId, out var tag))
                throw new ValidationException($"Activity tag {tagId} was not found.");

            tags.Add(tag);
        }

        if (tags.Any(t => t.IsCustom) && string.IsNullOrWhiteSpace(entryDto.CustomText))
            throw new ValidationException("Custom text is required when the Other activity tag is selected.");

        return tags;
    }
}
