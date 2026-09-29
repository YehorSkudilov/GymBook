using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class WarmupsAndRest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Warmups",
                table: "profiles",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompoundRestSeconds",
                table: "plans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsolationRestSeconds",
                table: "plans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warmups",
                table: "plans",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Warmups",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "CompoundRestSeconds",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "IsolationRestSeconds",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "Warmups",
                table: "plans");
        }
    }
}
