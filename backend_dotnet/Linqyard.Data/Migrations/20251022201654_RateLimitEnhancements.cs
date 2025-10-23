using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linqyard.Data.Migrations
{
    /// <inheritdoc />
    public partial class RateLimitEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RateLimitBuckets_Key_WindowStart",
                table: "RateLimitBuckets");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "RateLimitBuckets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitBuckets_Key_WindowStart",
                table: "RateLimitBuckets",
                columns: new[] { "Key", "WindowStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RateLimitBuckets_Key_WindowStart",
                table: "RateLimitBuckets");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "RateLimitBuckets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitBuckets_Key_WindowStart",
                table: "RateLimitBuckets",
                columns: new[] { "Key", "WindowStart" });
        }
    }
}
