using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TesisatTeklifApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "PaymentPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidDate",
                table: "PaymentPlans",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "PaymentPlans");
        }
    }
}
