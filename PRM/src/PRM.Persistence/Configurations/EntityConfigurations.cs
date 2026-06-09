using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.FullName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Department).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.User).WithOne(x => x.EmployeeProfile).HasForeignKey<Employee>(x => x.UserId);
        builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).IsRequired(false);
        builder.HasMany(x => x.Skills).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        builder.HasMany(x => x.Allocations).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
    }
}

public class EmployeeSkillConfiguration : IEntityTypeConfiguration<EmployeeSkill>
{
    public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
    {
        builder.ToTable("employee_skills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.EmployeeId, x.Name }).IsUnique();
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.HealthReason).HasMaxLength(1024);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
        builder.HasMany(x => x.Milestones).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId);
        builder.HasMany(x => x.Allocations).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId);
    }
}

public class ProjectMilestoneConfiguration : IEntityTypeConfiguration<ProjectMilestone>
{
    public void Configure(EntityTypeBuilder<ProjectMilestone> builder)
    {
        builder.ToTable("milestones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UtilisationPercentage).HasPrecision(5, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("timesheets");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EmployeeId, x.WeekStart }).IsUnique();
        builder.Property(x => x.TotalHours).HasPrecision(6, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Entries).WithOne(x => x.Timesheet).HasForeignKey(x => x.TimesheetId);
    }
}

public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
    {
        builder.ToTable("timesheet_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hours).HasPrecision(6, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        builder.HasMany(x => x.ActivityTags).WithMany();
    }
}

public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>
{
    public void Configure(EntityTypeBuilder<ActivityTag> builder)
    {
        builder.ToTable("activity_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    }
}

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.ToTable("system_config");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LlmApiKeyEncrypted).HasMaxLength(1024);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2048);
    }
}
