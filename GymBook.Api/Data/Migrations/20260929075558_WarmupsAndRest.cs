using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
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
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompoundRestSeconds",
                table: "plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsolationRestSeconds",
                table: "plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warmups",
                table: "plans",
                type: "character varying(4000)",
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
