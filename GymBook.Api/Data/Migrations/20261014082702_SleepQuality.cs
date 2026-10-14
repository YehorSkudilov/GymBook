using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SleepQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SleepDeepMinutes",
                table: "health_days",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepRemMinutes",
                table: "health_days",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepScore",
                table: "health_days",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SleepDeepMinutes",
                table: "health_days");

            migrationBuilder.DropColumn(
                name: "SleepRemMinutes",
                table: "health_days");

            migrationBuilder.DropColumn(
                name: "SleepScore",
                table: "health_days");
        }
    }
}
