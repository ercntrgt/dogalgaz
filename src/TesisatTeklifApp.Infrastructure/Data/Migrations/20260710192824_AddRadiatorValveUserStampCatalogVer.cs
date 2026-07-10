using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TesisatTeklifApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiatorValveUserStampCatalogVer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatalogVersion",
                table: "StockSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsValve",
                table: "RadiatorItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "RadiatorItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "RadiatorItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "RadiatorItems",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SignatureStamp",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogVersion",
                table: "StockSettings");

            migrationBuilder.DropColumn(
                name: "IsValve",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "SignatureStamp",
                table: "AspNetUsers");
        }
    }
}
