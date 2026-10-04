using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>The home gym is gone from public profiles. Written by hand, without a Designer file: the attributes that
    /// file would carry are here, and the model snapshot was edited to match.</remarks>
    [DbContext(typeof(ApiDbContext))]
    [Migration("20261009090000_DropHomeGym")]
    public partial class DropHomeGym : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeGym",
                table: "social_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeGym",
                table: "social_profiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
