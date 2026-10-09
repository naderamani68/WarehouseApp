using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Group", "Hint", "SortOrder", "Title", "UpdatedAt", "Username", "Value", "ValueType" },
                values: new object[,]
                {
                    { "Sms.ApiKey", "پیامک", null, 3, "کلید پنل پیامک", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "", "text" },
                    { "Sms.ApiUrl", "پیامک", "جانگهدارها: {apiKey} و {sender} و {receptor} و {message}", 2, "نشانی پنل پیامک", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "", "text" },
                    { "Sms.Enabled", "پیامک", "تا خاموش باشد هیچ پیامکی فرستاده نمی‌شود", 1, "ارسال پیامک", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "false", "bool" },
                    { "Sms.Sender", "پیامک", null, 4, "شمارهٔ فرستنده", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "", "text" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Sms.ApiKey");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Sms.ApiUrl");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Sms.Enabled");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Sms.Sender");
        }
    }
}
