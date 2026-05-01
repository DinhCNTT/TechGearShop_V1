using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechGearShop_V1.Migrations
{
    /// <inheritdoc />
    public partial class AddStockNonNegativeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GuestEmail",
                table: "StockSubscriptions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_Products_Stock_NonNegative",
                table: "Products",
                sql: "[Stock] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_Products_Stock_NonNegative",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "GuestEmail",
                table: "StockSubscriptions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
