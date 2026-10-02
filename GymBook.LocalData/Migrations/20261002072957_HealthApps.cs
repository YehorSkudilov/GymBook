using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class HealthApps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthApps",
                table: "profiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthApps",
                table: "profiles");
        }
    }
}
