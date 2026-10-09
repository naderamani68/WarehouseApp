using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Group", "Hint", "SortOrder", "Title", "UpdatedAt", "Username", "Value", "ValueType" },
                values: new object[,]
                {
                    { "Backup.Folder", "پشتیبان‌گیری", null, 1, "پوشهٔ پشتیبان‌گیری (خالی = پوشهٔ backups کنار برنامه)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "", "text" },
                    { "Backup.KeepDays", "پشتیبان‌گیری", null, 2, "نگهداری پشتیبان (روز)", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "30", "number" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Backup.Folder");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Backup.KeepDays");
        }
    }
}
