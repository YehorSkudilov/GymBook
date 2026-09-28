using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRestSeconds",
                table: "profiles");

            migrationBuilder.AddColumn<int>(
                name: "CompoundRestSeconds",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsolationRestSeconds",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarmupRestSeconds",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompoundRestSeconds",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "IsolationRestSeconds",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "WarmupRestSeconds",
                table: "profiles");

            migrationBuilder.AddColumn<int>(
                name: "DefaultRestSeconds",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 120);
        }
    }
}
