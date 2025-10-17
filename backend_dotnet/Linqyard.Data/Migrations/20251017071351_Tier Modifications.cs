using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linqyard.Data.Migrations
{
    /// <inheritdoc />
    public partial class TierModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TierId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierId = table.Column<int>(type: "integer", nullable: false),
                    ActiveFrom = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ActiveUntil = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTiers_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTiers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTiers_TierId",
                table: "UserTiers",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTiers_UserId_ActiveFrom",
                table: "UserTiers",
                columns: new[] { "UserId", "ActiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTiers_UserId_ActiveUntil",
                table: "UserTiers",
                columns: new[] { "UserId", "ActiveUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTiers_UserId_IsActive",
                table: "UserTiers",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTiers");

            migrationBuilder.AddColumn<int>(
                name: "TierId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
