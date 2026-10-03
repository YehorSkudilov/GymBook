using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.LocalData.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// My foods gained a brand, what a serving is and its weight. They live in the profile's MyFoods jsonb column, so
    /// there's nothing to change in the database: only the model.
    /// </remarks>
    public partial class SavedFoodServing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
