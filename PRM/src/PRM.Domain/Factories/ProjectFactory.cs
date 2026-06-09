using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Domain.Factories;

public static class ProjectFactory
{
    public static Project Create(
        string name,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        ProjectStatus status,
        long managerId,
        int totalStoryPoints,
        long createdBy,
        DateTime utcNow) =>
        Project.Create(name, description, startDate, endDate, status, managerId, totalStoryPoints, createdBy, utcNow);
}
