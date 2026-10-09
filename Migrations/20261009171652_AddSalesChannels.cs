using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                table: "SalesOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesChannels", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SalesChannels",
                columns: new[] { "Id", "CreatedAt", "Hint", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "فروش رودررو به مشتری", true, "حضوری", 1 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "سفارش تلفنی", true, "تلفنی", 2 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "آگهی در دیوار", true, "دیوار", 3 },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "دایرکت یا صفحهٔ اینستاگرام", true, "اینستاگرام", 4 },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "فروش در دیجی‌کالا", true, "دیجی‌کالا", 5 },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "سفارش از سایت خودمان", true, "فروشگاه اینترنتی", 6 },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "هر جای دیگر", true, "سایر", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ChannelId",
                table: "SalesOrders",
                column: "ChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_SalesChannels_ChannelId",
                table: "SalesOrders",
                column: "ChannelId",
                principalTable: "SalesChannels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_SalesChannels_ChannelId",
                table: "SalesOrders");

            migrationBuilder.DropTable(
                name: "SalesChannels");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_ChannelId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "SalesOrders");
        }
    }
}
