using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linqyard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLinks2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_Sequence",
                table: "Links");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "LinkGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinkGroups_UserId_Sequence",
                table: "LinkGroups",
                columns: new[] { "UserId", "Sequence" });

            migrationBuilder.AddForeignKey(
                name: "FK_LinkGroups_Users_UserId",
                table: "LinkGroups",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkGroups_Users_UserId",
                table: "LinkGroups");

            migrationBuilder.DropIndex(
                name: "IX_LinkGroups_UserId_Sequence",
                table: "LinkGroups");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LinkGroups");

            migrationBuilder.CreateIndex(
                name: "IX_Links_Sequence",
                table: "Links",
                column: "Sequence");
        }
    }
}
