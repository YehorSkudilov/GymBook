using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <summary>
    /// Brings plan_generations up to date on databases that got the first version of the PlanGenerations migration,
    /// before it gained the Kind column (it was changed in place). Idempotent, so databases created from the current
    /// version are left as they are.
    /// </summary>
    public partial class PlanGenerationKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE plan_generations ADD COLUMN IF NOT EXISTS "Kind" character varying(16) NOT NULL DEFAULT 'plan';
                ALTER TABLE plan_generations ALTER COLUMN "Kind" DROP DEFAULT;
                DROP INDEX IF EXISTS "IX_plan_generations_UserId_CreatedAt";
                CREATE INDEX IF NOT EXISTS "IX_plan_generations_UserId_Kind_CreatedAt" ON plan_generations ("UserId", "Kind", "CreatedAt");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
