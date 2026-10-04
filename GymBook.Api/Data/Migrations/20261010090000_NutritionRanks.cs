using System;
using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>Food logging on public profiles, for the nutrition boards. Written by hand, without a Designer file: the
    /// attributes that file would carry are here, and the model snapshot was edited to match.</remarks>
    [DbContext(typeof(ApiDbContext))]
    [Migration("20261010090000_NutritionRanks")]
    public partial class NutritionRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FoodStreak",
                table: "social_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FoodBestStreak",
                table: "social_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FoodOnTarget",
                table: "social_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FoodUpdatedAt",
                table: "social_profiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FoodStreak", table: "social_profiles");
            migrationBuilder.DropColumn(name: "FoodBestStreak", table: "social_profiles");
            migrationBuilder.DropColumn(name: "FoodOnTarget", table: "social_profiles");
            migrationBuilder.DropColumn(name: "FoodUpdatedAt", table: "social_profiles");
        }
    }
}
