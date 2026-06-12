using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Constants;
using PRM.Domain.Entities;

namespace PRM.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RoleName).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.RoleName).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
        builder.HasMany(x => x.Permissions).WithOne(x => x.Role).HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            Role.Create(RoleIds.Admin, "admin", "System operator — manages master data and user accounts"),
            Role.Create(RoleIds.Manager, "manager", "Delivery manager — allocates resources and monitors projects"),
            Role.Create(RoleIds.Resource, "resource", "Individual contributor — submits timesheets and views own allocations"));
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Permission).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.RoleId, x.Permission }).IsUnique();

        builder.HasData(
            RolePermission.Create(1, RoleIds.Admin, RolePermissions.UsersManage),
            RolePermission.Create(2, RoleIds.Admin, RolePermissions.ResourceProfilesManage),
            RolePermission.Create(3, RoleIds.Admin, RolePermissions.ProjectsManage),
            RolePermission.Create(4, RoleIds.Admin, RolePermissions.AllocationsViewAll),
            RolePermission.Create(5, RoleIds.Admin, RolePermissions.SystemManage),

            RolePermission.Create(6, RoleIds.Manager, RolePermissions.DashboardView),
            RolePermission.Create(7, RoleIds.Manager, RolePermissions.AllocationsManage),
            RolePermission.Create(8, RoleIds.Manager, RolePermissions.ProjectsViewOwn),
            RolePermission.Create(9, RoleIds.Manager, RolePermissions.TimesheetsViewTeam),
            RolePermission.Create(10, RoleIds.Manager, RolePermissions.AiUse),

            RolePermission.Create(11, RoleIds.Resource, RolePermissions.TimesheetsSubmit),
            RolePermission.Create(12, RoleIds.Resource, RolePermissions.TimesheetsViewOwn),
            RolePermission.Create(13, RoleIds.Resource, RolePermissions.AllocationsViewOwn));
    }
}

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
        builder.Property(x => x.PasswordHash).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ResourceProfileConfiguration : IEntityTypeConfiguration<ResourceProfile>
{
    public void Configure(EntityTypeBuilder<ResourceProfile> builder)
    {
        builder.ToTable("resource_profiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.FullName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.User).WithOne(x => x.ResourceProfile).HasForeignKey<ResourceProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Skills).WithOne(x => x.ResourceProfile).HasForeignKey(x => x.ResourceProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Allocations).WithOne(x => x.ResourceProfile).HasForeignKey(x => x.ResourceProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ResourceProfileSkillConfiguration : IEntityTypeConfiguration<ResourceProfileSkill>
{
    public void Configure(EntityTypeBuilder<ResourceProfileSkill> builder)
    {
        builder.ToTable("resource_profile_skills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ResourceProfileId, x.Name }).IsUnique();
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
        builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Milestones).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Allocations).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.HasIndex(x => new { x.ResourceProfileId, x.WeekStart }).IsUnique();
        builder.Property(x => x.TotalHours).HasPrecision(6, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Entries).WithOne(x => x.Timesheet).HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
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
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ActivityTags).WithMany();
    }
}

public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>
{
    public void Configure(EntityTypeBuilder<ActivityTag> builder)
    {
        builder.ToTable("activity_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
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
