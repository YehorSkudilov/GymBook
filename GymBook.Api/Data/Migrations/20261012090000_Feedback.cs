using System;
using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Feature requests and bug reports: the tickets and comments live on GitHub, so only who owns which, and who voted,
    /// is kept here. Written by hand, without a Designer file: the attributes that file would carry are here, and the
    /// model snapshot was edited to match.
    /// </remarks>
    [DbContext(typeof(ApiDbContext))]
    [Migration("20261012090000_Feedback")]
    public partial class Feedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback_tickets",
                columns: table => new
                {
                    Number = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_tickets", x => x.Number);
                    table.ForeignKey(
                        name: "FK_feedback_tickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedback_comments",
                columns: table => new
                {
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_feedback_comments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedback_votes",
                columns: table => new
                {
                    Number = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_votes", x => new { x.Number, x.UserId });
                    table.ForeignKey(
                        name: "FK_feedback_votes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_tickets_UserId_Kind",
                table: "feedback_tickets",
                columns: new[] { "UserId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_comments_Number",
                table: "feedback_comments",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_comments_UserId",
                table: "feedback_comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_votes_UserId",
                table: "feedback_votes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "feedback_votes");
            migrationBuilder.DropTable(name: "feedback_comments");
            migrationBuilder.DropTable(name: "feedback_tickets");
        }
    }
}
