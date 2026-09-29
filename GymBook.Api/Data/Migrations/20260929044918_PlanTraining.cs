using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing plans keep working as before: RIR stays on, and deloads and periodization start off (new plans
            // get them on). They can be switched in the plan's training options.
            migrationBuilder.AddColumn<bool>(
                name: "Deloads",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Periodization",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseRir",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deloads",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "Periodization",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "UseRir",
                table: "plans");
        }
    }
}
