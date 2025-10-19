using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Linqyard.Data.Migrations
{
    /// <inheritdoc />
    public partial class TierBillingCyclesAndCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tiers",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Tiers",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TierId = table.Column<int>(type: "integer", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    RedemptionCount = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coupons_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TierBillingCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TierId = table.Column<int>(type: "integer", nullable: false),
                    BillingPeriod = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DurationMonths = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierBillingCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierBillingCycles_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DiscountPercentage", "IsActive", "MaxRedemptions", "RedemptionCount", "TierId", "UpdatedAt", "ValidFrom", "ValidUntil" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "WELCOME10", new DateTimeOffset(new DateTime(2025, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Introductory 10% discount for Plus tier", 10m, true, 500, 0, 2, new DateTimeOffset(new DateTime(2025, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "TierBillingCycles",
                columns: new[] { "Id", "Amount", "BillingPeriod", "Description", "DurationMonths", "IsActive", "TierId" },
                values: new object[,]
                {
                    { 1, 6900, "monthly", "Monthly subscription for Plus", 1, true, 2 },
                    { 2, 70000, "yearly", "Yearly subscription for Plus", 12, true, 2 },
                    { 3, 9900, "monthly", "Monthly subscription for Pro", 1, true, 3 },
                    { 4, 95000, "yearly", "Yearly subscription for Pro", 12, true, 3 }
                });

            migrationBuilder.Sql("SELECT setval('\"TierBillingCycles_Id_seq\"', 4, true);");

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Currency",
                value: "INR");

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Currency",
                value: "INR");

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 3,
                column: "Currency",
                value: "INR");

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_Name",
                table: "Tiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_TierId",
                table: "Coupons",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_TierBillingCycles_TierId_BillingPeriod",
                table: "TierBillingCycles",
                columns: new[] { "TierId", "BillingPeriod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "TierBillingCycles");

            migrationBuilder.DropIndex(
                name: "IX_Tiers_Name",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Tiers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tiers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);
        }
    }
}
