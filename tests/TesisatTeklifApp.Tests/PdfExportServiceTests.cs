using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class PdfExportServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public PdfExportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.StockSettings.Add(new StockSettings());
        ctx.SaveChanges();
    }

    [Fact]
    public async Task GeneratePdf_Produces_Valid_Pdf_With_Insufficient_Rows()
    {
        int offerId;
        using (var ctx = new AppDbContext(_options))
        {
            var cust = new Customer { FirstName = "Ahmet", LastName = "Yılmaz", City = "Isparta", Phone = "555" };
            var prod = new Product { Name = "Kombi", Category = ProductCategories.Kombi, IsStockTracked = true, StockQuantity = 1, SalePrice = 32000 };
            ctx.Customers.Add(cust);
            ctx.Products.Add(prod);
            await ctx.SaveChangesAsync();

            var offer = new Offer
            {
                OfferNumber = "TSF-2026-0001", CustomerId = cust.Id, Status = OfferStatus.ConvertedToOrder,
                OrderNumber = "SPR-2026-0001", VatRate = 20m, GeneralNotes = "Türkçe karakter testi: ğüşıöçĞÜŞİÖÇ"
            };
            offer.Items.Add(new OfferItem
            {
                ProductId = prod.Id, SectionName = OfferSections.KombiKazan, ItemName = "Kombi",
                Quantity = 5, RequestedQuantity = 5, UnitPrice = 32000, TotalPrice = 160000, Unit = "Adet", IsSelected = true
            });
            ctx.Offers.Add(offer);
            await ctx.SaveChangesAsync();
            offerId = offer.Id;
        }

        using var dbctx = new AppDbContext(_options);
        var pdf = new PdfExportService(dbctx, new StockControlService(dbctx), logoPath: null);
        var (fileName, content) = await pdf.GenerateOfferPdfAsync(offerId);

        Assert.True(content.Length > 1000, "PDF içeriği boş olmamalı");
        // PDF sihirli baytları: %PDF
        Assert.Equal((byte)'%', content[0]);
        Assert.Equal((byte)'P', content[1]);
        Assert.Equal((byte)'D', content[2]);
        Assert.Equal((byte)'F', content[3]);
        Assert.Contains("Siparis_Formu", fileName);
        Assert.EndsWith(".pdf", fileName);
    }

    public void Dispose() => _conn.Dispose();
}
