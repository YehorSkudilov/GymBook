using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class PlanSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // What new plans start with: deload weeks and periodization on, as they were before these could be changed.
            migrationBuilder.AddColumn<bool>(
                name: "DefaultDeloads",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultPeriodization",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // Plans with a rest time of their own keep them as their own; the rest follow the profile's, as before.
            migrationBuilder.AddColumn<bool>(
                name: "OwnRest",
                table: "plans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE plans SET \"OwnRest\" = 1 WHERE \"CompoundRestSeconds\" IS NOT NULL OR \"IsolationRestSeconds\" IS NOT NULL;");

            // Existing plans keep their own RIR, deloads and periodization; new ones follow the profile's defaults.
            migrationBuilder.AddColumn<bool>(
                name: "OwnTraining",
                table: "plans",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetRir",
                table: "plans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarmupRestSeconds",
                table: "plans",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDeloads",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "DefaultPeriodization",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "OwnRest",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "OwnTraining",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "TargetRir",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "WarmupRestSeconds",
                table: "plans");
        }
    }
}
