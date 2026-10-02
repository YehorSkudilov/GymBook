using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
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
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CalorieGoal",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarbsGoalG",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnergyBalanceGoal",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FatGoalG",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthSource",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ImportHealthFood",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ProteinGoalG",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sex",
                table: "profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BmrKcal",
                table: "body_weights",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyFatPercent",
                table: "body_weights",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BodyWaterKg",
                table: "body_weights",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BoneMassKg",
                table: "body_weights",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LeanMassKg",
                table: "body_weights",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "body_weights",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "food_entries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Meal = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Calories = table.Column<double>(type: "double precision", nullable: false),
                    ProteinG = table.Column<double>(type: "double precision", nullable: false),
                    CarbsG = table.Column<double>(type: "double precision", nullable: false),
                    FatG = table.Column<double>(type: "double precision", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_entries", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_food_entries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_days",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalBurnedKcal = table.Column<double>(type: "double precision", nullable: true),
                    ActiveBurnedKcal = table.Column<double>(type: "double precision", nullable: true),
                    BasalBurnedKcal = table.Column<double>(type: "double precision", nullable: true),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    FoodKcal = table.Column<double>(type: "double precision", nullable: true),
                    FoodProteinG = table.Column<double>(type: "double precision", nullable: true),
                    FoodCarbsG = table.Column<double>(type: "double precision", nullable: true),
                    FoodFatG = table.Column<double>(type: "double precision", nullable: true),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_days", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_health_days_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_food_entries_UserId_Version",
                table: "food_entries",
                columns: new[] { "UserId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_health_days_UserId_Version",
                table: "health_days",
                columns: new[] { "UserId", "Version" });
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
