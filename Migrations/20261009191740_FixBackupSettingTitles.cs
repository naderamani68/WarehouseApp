using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class FixBackupSettingTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // اصلاح ردیف‌هایی که با دستور دستی ساخته شده بودند و متن فارسی‌شان خراب شده بود
            migrationBuilder.Sql("UPDATE AppSettings SET [Group] = N'پشتیبان‌گیری', Title = N'پشتیبان‌گیری خودکار روزانه', ValueType = 'bool', SortOrder = 10, Hint = N'هر روز در ساعت تعیین‌شده خودکار پشتیبان می‌گیرد' WHERE [Key] = 'Backup.AutoEnabled'");
            migrationBuilder.Sql("UPDATE AppSettings SET [Group] = N'پشتیبان‌گیری', Title = N'ساعت اجرای پشتیبان‌گیری خودکار', ValueType = 'number', SortOrder = 11, Hint = N'عددی از صفر تا ۲۳' WHERE [Key] = 'Backup.AutoHour'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
