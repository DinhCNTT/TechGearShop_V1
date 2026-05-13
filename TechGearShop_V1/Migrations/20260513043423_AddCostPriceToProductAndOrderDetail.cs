using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechGearShop_V1.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceToProductAndOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostPrice",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Tự động set Giá vốn = 70% Giá bán cho các sản phẩm đã có trong DB
            migrationBuilder.Sql("UPDATE Products SET CostPrice = Price * 0.7");
            // Set UnitCostPrice cho các OrderDetail cũ = UnitPrice * 0.7
            migrationBuilder.Sql("UPDATE OrderDetails SET UnitCostPrice = UnitPrice * 0.7");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitCostPrice",
                table: "OrderDetails");
        }
    }
}
