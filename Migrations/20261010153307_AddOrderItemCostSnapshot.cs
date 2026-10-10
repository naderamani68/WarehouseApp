using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemCostSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "SalesOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // برای سفارش‌های قبلی، بهای تمام‌شدهٔ فعلی کالا عکس گرفته می‌شود
            // تا از این پس با تغییر قیمت خرید، سود سفارش‌های قدیمی عوض نشود
            migrationBuilder.Sql(
                "UPDATE [SalesOrderItems] SET [CostPrice] = ISNULL((SELECT [CostPrice] FROM [Products] WHERE [Products].[Id] = [SalesOrderItems].[ProductId]), 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "SalesOrderItems");
        }
    }
}
