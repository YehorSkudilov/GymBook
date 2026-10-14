using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepRemMinutes",
                table: "health_days",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SleepScore",
                table: "health_days",
                type: "INTEGER",
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
