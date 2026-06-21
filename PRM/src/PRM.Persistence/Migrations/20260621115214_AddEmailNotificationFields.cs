using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "MissedTimesheetWeekStart",
                table: "resource_profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesheetReminderCount",
                table: "resource_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TimesheetSubmissionFrozen",
                table: "resource_profiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtRiskNotificationSentAt",
                table: "projects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MissedTimesheetWeekStart",
                table: "resource_profiles");

            migrationBuilder.DropColumn(
                name: "TimesheetReminderCount",
                table: "resource_profiles");

            migrationBuilder.DropColumn(
                name: "TimesheetSubmissionFrozen",
                table: "resource_profiles");

            migrationBuilder.DropColumn(
                name: "AtRiskNotificationSentAt",
                table: "projects");
        }
    }
}
