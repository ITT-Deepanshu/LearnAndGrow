namespace PRM.Application.Allocations;

public sealed record CreateAllocationDto(
    long ProjectId,
    long ResourceProfileId,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record AllocationDto(
    long Id,
    long ResourceProfileId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly? EndedAt);

public sealed record AllocationListItemDto(
    long Id,
    long ResourceProfileId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);
