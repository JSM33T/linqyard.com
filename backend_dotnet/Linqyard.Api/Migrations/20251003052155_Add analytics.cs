using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linqyard.Api.Migrations
{
    /// <inheritdoc />
    public partial class Addanalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<IPAddress>(type: "inet", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Analytics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_At",
                table: "Analytics",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_LinkId",
                table: "Analytics",
                column: "LinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_UserId",
                table: "Analytics",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analytics");
        }
    }
}
