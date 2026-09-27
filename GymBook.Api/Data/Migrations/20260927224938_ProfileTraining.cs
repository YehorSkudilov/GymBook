using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfileTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthYear",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyFatPercent",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainNeck",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingSince",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthYear",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "BodyFatPercent",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "TrainNeck",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "TrainingSince",
                table: "profiles");
        }
    }
}
