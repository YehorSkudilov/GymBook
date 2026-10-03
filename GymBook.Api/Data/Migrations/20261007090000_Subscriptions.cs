using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymBook.Api.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>Gym Book Pro: store subscriptions checked with Google Play, the App Store and the Microsoft Store.</remarks>
    public partial class Subscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Store = table.Column<int>(type: "integer", nullable: false),
                    PurchaseKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    Environment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ProductId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AutoRenewing = table.Column<bool>(type: "boolean", nullable: false),
                    InTrial = table.Column<bool>(type: "boolean", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Store_PurchaseKey",
                table: "subscriptions",
                columns: new[] { "Store", "PurchaseKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_ExpiresAt",
                table: "subscriptions",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "subscriptions");
        }
    }
}
