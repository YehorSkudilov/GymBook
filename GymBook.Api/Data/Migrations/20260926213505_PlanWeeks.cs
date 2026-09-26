using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanWeeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanWeek",
                table: "sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "RestDaysDone",
                table: "plans",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanWeek",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "RestDaysDone",
                table: "plans");
        }
    }
}
