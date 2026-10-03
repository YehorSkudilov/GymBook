using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Health days gained the minutes slept in the night woken up from on them (from Samsung Health or Health Connect),
    /// used to work out recovery.
    /// </remarks>
    public partial class HealthSleep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SleepMinutes",
                table: "health_days",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SleepMinutes",
                table: "health_days");
        }
    }
}
