using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    public partial class Nutrition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityLevel",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CalorieGoal",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarbsGoalG",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnergyBalanceGoal",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FatGoalG",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthSource",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "profiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ImportHealthFood",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ProteinGoalG",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sex",
                table: "profiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BmrKcal",
                table: "body_weights",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyFatPercent",
                table: "body_weights",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyWaterKg",
                table: "body_weights",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BoneMassKg",
                table: "body_weights",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LeanMassKg",
                table: "body_weights",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "body_weights",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "food_entries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Meal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Calories = table.Column<double>(type: "REAL", nullable: false),
                    ProteinG = table.Column<double>(type: "REAL", nullable: false),
                    CarbsG = table.Column<double>(type: "REAL", nullable: false),
                    FatG = table.Column<double>(type: "REAL", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dirty = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "health_days",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalBurnedKcal = table.Column<double>(type: "REAL", nullable: true),
                    ActiveBurnedKcal = table.Column<double>(type: "REAL", nullable: true),
                    BasalBurnedKcal = table.Column<double>(type: "REAL", nullable: true),
                    Steps = table.Column<int>(type: "INTEGER", nullable: true),
                    FoodKcal = table.Column<double>(type: "REAL", nullable: true),
                    FoodProteinG = table.Column<double>(type: "REAL", nullable: true),
                    FoodCarbsG = table.Column<double>(type: "REAL", nullable: true),
                    FoodFatG = table.Column<double>(type: "REAL", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Dirty = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_days", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "food_entries");

            migrationBuilder.DropTable(
                name: "health_days");

            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "CalorieGoal",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "CarbsGoalG",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "EnergyBalanceGoal",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "FatGoalG",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "HealthSource",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "ImportHealthFood",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "ProteinGoalG",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "BmrKcal",
                table: "body_weights");

            migrationBuilder.DropColumn(
                name: "BodyFatPercent",
                table: "body_weights");

            migrationBuilder.DropColumn(
                name: "BodyWaterKg",
                table: "body_weights");

            migrationBuilder.DropColumn(
                name: "BoneMassKg",
                table: "body_weights");

            migrationBuilder.DropColumn(
                name: "LeanMassKg",
                table: "body_weights");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "body_weights");
        }
    }
}
