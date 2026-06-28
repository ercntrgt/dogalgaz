using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TesisatTeklifApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Offers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Offers");
        }
    }
}
