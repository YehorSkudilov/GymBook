using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyFatPercent",
                table: "profiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainNeck",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingSince",
                table: "profiles",
                type: "TEXT",
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
