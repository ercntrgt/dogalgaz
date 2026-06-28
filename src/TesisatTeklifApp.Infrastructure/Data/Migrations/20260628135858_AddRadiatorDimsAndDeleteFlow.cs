using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TesisatTeklifApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiatorDimsAndDeleteFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RadiatorHeight",
                table: "RadiatorItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadiatorWidth",
                table: "RadiatorItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeleteRequested",
                table: "Offers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteRequestedBy",
                table: "Offers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RadiatorHeight",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "RadiatorWidth",
                table: "RadiatorItems");

            migrationBuilder.DropColumn(
                name: "DeleteRequested",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "DeleteRequestedBy",
                table: "Offers");
        }
    }
}
