using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ۱) ستون نقش در جدول کارمندان (فعلاً خالی)
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Employees",
                type: "int",
                nullable: true);

            // ۲) جدول نقش‌ها
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Permissions = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            // ۳) نقش‌های پیش‌فرض سیستم
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "IsActive", "IsSystemRole", "Name", "Permissions" },
                values: new object[,]
                {
                    { 1, "دسترسی کامل به همهٔ بخش‌ها", true, true, "مدیر سیستم", 8191 },
                    { 2, "همهٔ عملیات انبار و گزارش‌ها، بدون مدیریت کارمندان", true, true, "مدیر انبار", 5119 },
                    { 3, "ثبت حرکات، مدیریت محصولات و دیدن گزارش‌ها", true, true, "انباردار", 899 },
                    { 4, "فقط مشاهده", true, true, "بازدیدکننده", 513 }
                });

            // ۴) انتقال نقش کاربران فعلی به جدول جدید (۱=مدیر سیستم … ۴=بازدیدکننده)
            //    این کار باید پیش از حذف ستون قدیمی انجام شود تا داده‌ها از دست نروند.
            migrationBuilder.Sql("UPDATE Employees SET RoleId = Role WHERE RoleId IS NULL;");

            // ۵) حذف ستون قدیمی نقش
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Employees");

            // ۶) ایندکس‌ها و کلید خارجی
            migrationBuilder.CreateIndex(
                name: "IX_Employees_RoleId",
                table: "Employees",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Roles_RoleId",
                table: "Employees",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Roles_RoleId",
                table: "Employees");

            // ستون قدیمی را برمی‌گردانیم و مقدارش را از RoleId پر می‌کنیم
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Employees SET Role = ISNULL(RoleId, 4);");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Employees_RoleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Employees");
        }
    }
}
