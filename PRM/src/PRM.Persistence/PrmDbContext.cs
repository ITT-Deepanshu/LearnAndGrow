using Microsoft.EntityFrameworkCore;
using PRM.Domain.Entities;

namespace PRM.Persistence;

public class PrmDbContext(DbContextOptions<PrmDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ResourceProfile> ResourceProfiles => Set<ResourceProfile>();
    public DbSet<ResourceProfileSkill> ResourceProfileSkills => Set<ResourceProfileSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrmDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
