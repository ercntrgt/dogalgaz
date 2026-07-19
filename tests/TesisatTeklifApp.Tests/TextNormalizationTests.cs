using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;
using Xunit;

namespace TesisatTeklifApp.Tests;

/// <summary>
/// Kullanıcı girdisi metinler DB'ye Türkçe büyük harfle yazılır; e-posta/token gibi
/// alanlar korunur. Kural AppDbContext.NormalizeStrings + TextCasing.UpperFields'ta.
/// </summary>
public class TextNormalizationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public TextNormalizationTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private AppDbContext NewCtx() => new(_options);

    [Theory]
    [InlineData("içel", "İÇEL")]          // i → İ (invariant'ta yanlışlıkla I olurdu)
    [InlineData("ısparta", "ISPARTA")]    // ı → I
    [InlineData("Ahmet Yılmaz", "AHMET YILMAZ")]
    [InlineData("şişli", "ŞİŞLİ")]
    public void TrUpper_Uses_Turkish_Rules(string input, string expected) =>
        Assert.Equal(expected, TextCasing.TrUpper(input));

    [Fact]
    public async Task Customer_Text_Fields_Are_Stored_Uppercase()
    {
        int id;
        using (var ctx = NewCtx())
        {
            var c = new Customer
            {
                FirstName = "ali", LastName = "çelik",
                City = "içel", District = "mezitli", Address = "atatürk cd. no:5"
            };
            ctx.Customers.Add(c);
            await ctx.SaveChangesAsync();
            id = c.Id;
        }

        using (var ctx = NewCtx())
        {
            var c = await ctx.Customers.FirstAsync(x => x.Id == id);
            Assert.Equal("ALİ", c.FirstName);
            Assert.Equal("ÇELİK", c.LastName);
            Assert.Equal("İÇEL", c.City);
            Assert.Equal("MEZİTLİ", c.District);
            Assert.Equal("ATATÜRK CD. NO:5", c.Address);
        }
    }

    [Fact]
    public async Task Product_Name_Uppercased_But_Category_And_Unit_Untouched()
    {
        int id;
        using (var ctx = NewCtx())
        {
            // Kategori/Birim sabit listelerden gelir; büyük harfe çevrilirse eşleşmeler bozulur.
            var p = new Product { Name = "kombi cihazı", Category = ProductCategories.Kombi, Unit = "Adet" };
            ctx.Products.Add(p);
            await ctx.SaveChangesAsync();
            id = p.Id;
        }

        using (var ctx = NewCtx())
        {
            var p = await ctx.Products.FirstAsync(x => x.Id == id);
            Assert.Equal("KOMBİ CİHAZI", p.Name);
            Assert.Equal(ProductCategories.Kombi, p.Category);
            Assert.Equal("Adet", p.Unit);
        }
    }

    [Fact]
    public async Task Email_Token_And_Signature_Are_Not_Uppercased()
    {
        int supplierId, offerId;
        using (var ctx = NewCtx())
        {
            var s = new Supplier { Name = "abc ltd", Email = "info@Abc.com" };
            ctx.Suppliers.Add(s);

            var c = new Customer { FirstName = "a", LastName = "b" };
            ctx.Customers.Add(c);
            await ctx.SaveChangesAsync();

            var o = new Offer
            {
                OfferNumber = "TSF-2026-0001", CustomerId = c.Id,
                PublicToken = "aB3xY9z", CustomerSignature = "data:image/png;base64,iVBORw0KGgo=",
                CreatedBy = "satis@ozdemir.local"
            };
            ctx.Offers.Add(o);
            await ctx.SaveChangesAsync();
            supplierId = s.Id; offerId = o.Id;
        }

        using (var ctx = NewCtx())
        {
            var s = await ctx.Suppliers.FirstAsync(x => x.Id == supplierId);
            Assert.Equal("ABC LTD", s.Name);          // ad çevrilir
            Assert.Equal("info@Abc.com", s.Email);    // e-posta korunur

            var o = await ctx.Offers.FirstAsync(x => x.Id == offerId);
            Assert.Equal("aB3xY9z", o.PublicToken);                              // token korunur
            Assert.Equal("data:image/png;base64,iVBORw0KGgo=", o.CustomerSignature); // imza korunur
            Assert.Equal("satis@ozdemir.local", o.CreatedBy);                    // e-posta korunur
        }
    }

    public void Dispose() => _conn.Dispose();
}
