using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVatSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatPercent",
                table: "SalesOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE [SalesOrders] SET [VatPercent] = CASE WHEN (SELECT [Value] FROM [AppSettings] WHERE [Key] = 'Tax.VatEnabled') = 'true' THEN ISNULL(TRY_CAST((SELECT [Value] FROM [AppSettings] WHERE [Key] = 'Tax.VatPercent') AS decimal(18,2)), 0) ELSE 0 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatPercent",
                table: "SalesOrders");
        }
    }
}
