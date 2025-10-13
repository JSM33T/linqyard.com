using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linqyard.Data.Migrations
{
    /// <inheritdoc />
    public partial class ViewTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ViewTelemetries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fingerprint = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Referrer = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UtmParameters = table.Column<string>(type: "jsonb", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Os = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<IPAddress>(type: "inet", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    ViewedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewTelemetries_Users_ProfileUserId",
                        column: x => x.ProfileUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewTelemetries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ViewTelemetries_Users_ViewerUserId",
                        column: x => x.ViewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_Fingerprint",
                table: "ViewTelemetries",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_ProfileUserId",
                table: "ViewTelemetries",
                column: "ProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_ProfileUserId_Source",
                table: "ViewTelemetries",
                columns: new[] { "ProfileUserId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_ProfileUserId_ViewedAt",
                table: "ViewTelemetries",
                columns: new[] { "ProfileUserId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_Source",
                table: "ViewTelemetries",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_UserId",
                table: "ViewTelemetries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_UtmParameters",
                table: "ViewTelemetries",
                column: "UtmParameters")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_ViewedAt",
                table: "ViewTelemetries",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ViewTelemetries_ViewerUserId",
                table: "ViewTelemetries",
                column: "ViewerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ViewTelemetries");
        }
    }
}
