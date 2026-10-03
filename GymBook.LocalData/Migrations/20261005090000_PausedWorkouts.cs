using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Sessions gained when they were paused (put aside unfinished, to be resumed later) and when a finished one was
    /// last reopened to carry on with it.
    /// </remarks>
    public partial class PausedWorkouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAt",
                table: "sessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                table: "sessions");
        }
    }
}
