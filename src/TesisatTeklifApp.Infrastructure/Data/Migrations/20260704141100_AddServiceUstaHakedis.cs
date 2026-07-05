using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TesisatTeklifApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceUstaHakedis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerApprovedDate",
                table: "Offers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "Offers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UstaEarning",
                table: "Offers",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UstaId",
                table: "Offers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "OfferItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RepairDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ServicedProductId = table.Column<int>(type: "INTEGER", nullable: true),
                    DeviceBrand = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceModel = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: true),
                    ComplaintSubject = table.Column<string>(type: "TEXT", nullable: true),
                    WorkDone = table.Column<string>(type: "TEXT", nullable: true),
                    SpecialNote = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceReasons = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TechnicianName = table.Column<string>(type: "TEXT", nullable: true),
                    TechnicianSignature = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerSignature = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Products_ServicedProductId",
                        column: x => x.ServicedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Ustalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Specialty = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ustalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UstaPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UstaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UstaPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UstaPayments_Ustalar_UstaId",
                        column: x => x.UstaId,
                        principalTable: "Ustalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_PublicToken",
                table: "Offers",
                column: "PublicToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_UstaId",
                table: "Offers",
                column: "UstaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_CustomerId",
                table: "ServiceRecords",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServicedProductId",
                table: "ServiceRecords",
                column: "ServicedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceNumber",
                table: "ServiceRecords",
                column: "ServiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UstaPayments_UstaId",
                table: "UstaPayments",
                column: "UstaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Ustalar_UstaId",
                table: "Offers",
                column: "UstaId",
                principalTable: "Ustalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Ustalar_UstaId",
                table: "Offers");

            migrationBuilder.DropTable(
                name: "ServiceRecords");

            migrationBuilder.DropTable(
                name: "UstaPayments");

            migrationBuilder.DropTable(
                name: "Ustalar");

            migrationBuilder.DropIndex(
                name: "IX_Offers_PublicToken",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_UstaId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CustomerApprovedDate",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "UstaEarning",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "UstaId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "OfferItems");
        }
    }
}
