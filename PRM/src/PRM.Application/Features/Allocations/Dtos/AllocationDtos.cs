namespace PRM.Application.Features.Allocations.Dtos;

public sealed record CreateAllocationDto(
    long ProjectId,
    long EmployeeId,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record AllocationDto(
    long Id,
    long EmployeeId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly? EndedAt);

public sealed record AllocationListItemDto(
    long Id,
    long EmployeeId,
    string EmployeeName,
    long ProjectId,
    string ProjectName,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate);
