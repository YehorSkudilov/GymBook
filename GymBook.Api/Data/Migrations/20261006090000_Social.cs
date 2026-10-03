using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// The social side: public profiles (username, picture, bio), strength ranks, friends, reports, and shared plans,
    /// whose plans now say which share they're in.
    /// </remarks>
    public partial class Social : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareId",
                table: "plans",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShareRole",
                table: "plans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avatars",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avatars", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_avatars_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FriendId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friendships", x => new { x.UserId, x.FriendId });
                    table.ForeignKey(
                        name: "FK_friendships_AspNetUsers_FriendId",
                        column: x => x.FriendId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friendships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_shares",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCopy = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_shares_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rank_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    Lift = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rank_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rank_reports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rank_reports_AspNetUsers_TargetId",
                        column: x => x.TargetId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ranked_lifts",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Lift = table.Column<int>(type: "integer", nullable: false),
                    Ranked = table.Column<bool>(type: "boolean", nullable: false),
                    E1RmKg = table.Column<double>(type: "double precision", nullable: false),
                    BodyWeightKg = table.Column<double>(type: "double precision", nullable: false),
                    Ratio = table.Column<double>(type: "double precision", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Sessions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranked_lifts", x => new { x.UserId, x.Lift });
                    table.ForeignKey(
                        name: "FK_ranked_lifts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_profiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UsernameKey = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Bio = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HomeGym = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    AvatarVersion = table.Column<int>(type: "integer", nullable: false),
                    FriendCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    InRanks = table.Column<bool>(type: "boolean", nullable: false),
                    RankSex = table.Column<int>(type: "integer", nullable: true),
                    RankHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    RanksUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_profiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_social_profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_share_members",
                columns: table => new
                {
                    ShareId = table.Column<string>(type: "character varying(64)", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_share_members", x => new { x.ShareId, x.UserId });
                    table.ForeignKey(
                        name: "FK_plan_share_members_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plan_share_members_plan_shares_ShareId",
                        column: x => x.ShareId,
                        principalTable: "plan_shares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_friendships_FriendId",
                table: "friendships",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_share_members_UserId",
                table: "plan_share_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_shares_OwnerId_PlanId",
                table: "plan_shares",
                columns: new[] { "OwnerId", "PlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rank_reports_ReporterId_TargetId",
                table: "rank_reports",
                columns: new[] { "ReporterId", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_rank_reports_TargetId_ResolvedAt",
                table: "rank_reports",
                columns: new[] { "TargetId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ranked_lifts_Lift_Score",
                table: "ranked_lifts",
                columns: new[] { "Lift", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_FriendCode",
                table: "social_profiles",
                column: "FriendCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_InRanks_RankHidden_Score",
                table: "social_profiles",
                columns: new[] { "InRanks", "RankHidden", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_UsernameKey",
                table: "social_profiles",
                column: "UsernameKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "avatars");
            migrationBuilder.DropTable(name: "friendships");
            migrationBuilder.DropTable(name: "plan_share_members");
            migrationBuilder.DropTable(name: "rank_reports");
            migrationBuilder.DropTable(name: "ranked_lifts");
            migrationBuilder.DropTable(name: "social_profiles");
            migrationBuilder.DropTable(name: "plan_shares");

            migrationBuilder.DropColumn(
                name: "ShareId",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "ShareRole",
                table: "plans");
        }
    }
}
