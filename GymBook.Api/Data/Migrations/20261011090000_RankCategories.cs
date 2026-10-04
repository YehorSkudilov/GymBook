using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ranks by category (six muscle groups from every weighted exercise, and consistency) instead of four lifts. The
    /// ranked_lifts "Lift" column now holds a RankCategory, and each row its best exercise. Old rows meant lifts, so
    /// they go and everyone's ranks are worked out again (the API does it when they're next asked for); reports keep
    /// pointing at what they meant (bench: chest, squat: legs, deadlift: posterior chain, press: shoulders). Written by
    /// hand, without a Designer file: the attributes that file would carry are here, and the model snapshot was edited
    /// to match.
    /// </remarks>
    [DbContext(typeof(ApiDbContext))]
    [Migration("20261011090000_RankCategories")]
    public partial class RankCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExerciseId",
                table: "ranked_lifts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""DELETE FROM ranked_lifts;""");
            migrationBuilder.Sql("""UPDATE social_profiles SET "RanksUpdatedAt" = NULL;""");
            migrationBuilder.Sql("""
                UPDATE rank_reports SET "Lift" = CASE "Lift" WHEN 0 THEN 0 WHEN 1 THEN 3 WHEN 2 THEN 4 WHEN 3 THEN 2 END
                WHERE "Lift" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ExerciseId", table: "ranked_lifts");
        }
    }
}
