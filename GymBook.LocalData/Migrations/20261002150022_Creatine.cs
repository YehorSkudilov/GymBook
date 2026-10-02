using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class Creatine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CreatineDoseG",
                table: "profiles",
                type: "REAL",
                nullable: false,
                defaultValue: 5.0);

            migrationBuilder.CreateTable(
                name: "supplement_doses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Grams = table.Column<double>(type: "REAL", nullable: false),
                    TakenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dirty = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplement_doses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplement_doses");

            migrationBuilder.DropColumn(
                name: "CreatineDoseG",
                table: "profiles");
        }
    }
}
