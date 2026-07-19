using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Services;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class OfferConversionTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly int _customerId;

    public OfferConversionTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.StockSettings.Add(new StockSettings());
        var c = new Customer { FirstName = "Ali", LastName = "Veli" };
        ctx.Customers.Add(c);
        ctx.SaveChanges();
        _customerId = c.Id;
    }

    private OfferService NewService(AppDbContext ctx) =>
        new(ctx, new OfferCalculationService(), new NumberGeneratorService(ctx), new StockControlService(ctx));

    [Fact]
    public async Task Convert_Updates_Same_Record_No_Duplicate()
    {
        int offerId;
        // 1) Taslak oluştur
        using (var ctx = new AppDbContext(_options))
        {
            var svc = NewService(ctx);
            var draft = new Offer { CustomerId = _customerId, Status = OfferStatus.Draft, VatRate = 20m };
            draft.Items.Add(new OfferItem { SectionName = OfferSections.Labor, ItemName = "İşçilik", Quantity = 1, UnitPrice = 1000, IsSelected = true });
            await svc.SaveAsync(draft);
            offerId = draft.Id;
        }

        // 2) Siparişe dönüştür
        using (var ctx = new AppDbContext(_options))
        {
            var svc = NewService(ctx);
            await svc.ConvertToOrderAsync(offerId);
        }

        // 3) Doğrula: TEK kayıt, durum ConvertedToOrder, sipariş no atanmış
        using (var ctx = new AppDbContext(_options))
        {
            var all = await ctx.Offers.ToListAsync();
            Assert.Single(all);                                   // kopya YOK
            Assert.Equal(OfferStatus.ConvertedToOrder, all[0].Status);
            Assert.False(string.IsNullOrEmpty(all[0].OrderNumber));
            Assert.Equal(offerId, all[0].Id);                     // aynı kayıt
        }
    }

    [Fact]
    public async Task Editing_Existing_Order_Keeps_Status_And_No_Duplicate()
    {
        int offerId;
        using (var ctx = new AppDbContext(_options))
        {
            var svc = NewService(ctx);
            var draft = new Offer { CustomerId = _customerId, Status = OfferStatus.Draft, VatRate = 20m };
            draft.Items.Add(new OfferItem { SectionName = OfferSections.Labor, ItemName = "İşçilik", Quantity = 1, UnitPrice = 1000, IsSelected = true });
            await svc.SaveAsync(draft);
            offerId = draft.Id;
        }
        using (var ctx = new AppDbContext(_options))
            await NewService(ctx).ConvertToOrderAsync(offerId);

        // Detached olarak yükle (UI gibi), düzenle, tekrar kaydet
        Offer reloaded;
        using (var ctx = new AppDbContext(_options))
            reloaded = (await NewService(ctx).GetByIdAsync(offerId))!;

        reloaded.GeneralNotes = "Düzenlendi";
        using (var ctx = new AppDbContext(_options))
            await NewService(ctx).SaveAsync(reloaded);

        using (var ctx = new AppDbContext(_options))
        {
            var all = await ctx.Offers.ToListAsync();
            Assert.Single(all);
            Assert.Equal(OfferStatus.ConvertedToOrder, all[0].Status);  // durum korunmalı
            // Serbest metinler DB'ye Türkçe büyük harfle yazılır (AppDbContext.NormalizeStrings).
            Assert.Equal("DÜZENLENDİ", all[0].GeneralNotes);
        }
    }

    [Fact]
    public async Task Convert_With_Shared_Context_Like_Blazor_Circuit_No_Duplicate()
    {
        // Blazor Server'da tek scoped DbContext form-kaydet, detay-yükle ve
        // dönüştürme arasında PAYLAŞILIR. Bu senaryoyu birebir taklit ediyoruz.
        using var ctx = new AppDbContext(_options);
        var svc = NewService(ctx);

        // 1) Formda yeni taslak kaydedilir (Id atanır, context bunu izler)
        var draft = new Offer { CustomerId = _customerId, Status = OfferStatus.Draft, VatRate = 20m };
        draft.Items.Add(new OfferItem { SectionName = OfferSections.KombiKazan, ItemName = "Kombi", Quantity = 1, UnitPrice = 32000, IsSelected = true });
        await svc.SaveAsync(draft);
        var id = draft.Id;

        // 2) Detay sayfası yüklenir (AsNoTracking)
        _ = await svc.GetByIdAsync(id);

        // 3) Siparişe dönüştürülür (aynı context)
        await svc.ConvertToOrderAsync(id);

        // 4) Detay yeniden yüklenir
        _ = await svc.GetByIdAsync(id);

        // Doğrula: hâlâ TEK kayıt, durum sipariş, taslak kalan yok
        var all = await ctx.Offers.IgnoreQueryFilters().ToListAsync();
        Assert.Single(all);
        Assert.Equal(OfferStatus.ConvertedToOrder, all[0].Status);
        Assert.Equal(0, all.Count(o => o.Status == OfferStatus.Draft));
    }

    [Fact]
    public async Task Save_Offer_With_Same_Product_In_Multiple_Items_Does_Not_Throw()
    {
        // Hatanın yeniden üretimi: aynı ürün birden çok satırda + yüklenmiş Product navigasyonu
        int productId, offerId;
        using (var ctx = new AppDbContext(_options))
        {
            var p = new Product { Name = "Boru", Category = ProductCategories.DogalgazBorulari, IsStockTracked = true, StockQuantity = 100, SalePrice = 50 };
            ctx.Products.Add(p);
            await ctx.SaveChangesAsync();
            productId = p.Id;

            var svc = NewService(ctx);
            var draft = new Offer { CustomerId = _customerId, Status = OfferStatus.Draft, VatRate = 20m };
            draft.Items.Add(new OfferItem { ProductId = productId, SectionName = OfferSections.GasInstallation, ItemName = "1\" boru", Quantity = 3, UnitPrice = 50, IsSelected = true });
            draft.Items.Add(new OfferItem { ProductId = productId, SectionName = OfferSections.GasInstallation, ItemName = "3/4\" boru", Quantity = 5, UnitPrice = 50, IsSelected = true });
            await svc.SaveAsync(draft);
            offerId = draft.Id;
        }

        // UI gibi: detached yükle (Product navigasyonları dolu gelir), düzenle, tekrar kaydet
        Offer reloaded;
        using (var ctx = new AppDbContext(_options))
            reloaded = (await NewService(ctx).GetByIdAsync(offerId))!;

        Assert.All(reloaded.Items, i => Assert.NotNull(i.Product)); // navigasyonlar dolu (hatayı tetikleyen durum)
        reloaded.GeneralNotes = "Güncellendi";

        using (var ctx = new AppDbContext(_options))
        {
            var ex = await Record.ExceptionAsync(() => NewService(ctx).SaveAsync(reloaded));
            Assert.Null(ex);   // artık hata fırlatmamalı
        }

        using (var ctx = new AppDbContext(_options))
        {
            var saved = await ctx.Offers.Include(o => o.Items).FirstAsync(o => o.Id == offerId);
            Assert.Equal(2, saved.Items.Count);
            Assert.All(saved.Items, i => Assert.Equal(productId, i.ProductId));
        }
    }

    [Fact]
    public async Task Save_With_Products_Already_Tracked_In_Context_Does_Not_Throw()
    {
        // Blazor circuit senaryosu: aynı context ürünleri izliyor (dropdown sorgusu),
        // sonra teklif kaydediliyor. ChangeTracker.Clear sayesinde çakışma olmamalı.
        using var ctx = new AppDbContext(_options);
        var p = new Product { Name = "Vana", Category = ProductCategories.RadyatorVanasi, IsStockTracked = true, StockQuantity = 10, SalePrice = 100 };
        ctx.Products.Add(p);
        await ctx.SaveChangesAsync();

        // Ürünleri izlenir halde tekrar yükle (eski dropdown davranışı / kirli context)
        var tracked = await ctx.Products.ToListAsync();
        Assert.NotEmpty(tracked);

        var svc = NewService(ctx);
        var offer = new Offer { CustomerId = _customerId, Status = OfferStatus.Draft, VatRate = 20m };
        offer.Items.Add(new OfferItem { ProductId = p.Id, SectionName = OfferSections.Material, ItemName = "Vana", Quantity = 2, UnitPrice = 100, IsSelected = true });
        offer.Items.Add(new OfferItem { ProductId = p.Id, SectionName = OfferSections.Material, ItemName = "Vana-2", Quantity = 3, UnitPrice = 100, IsSelected = true });

        var ex = await Record.ExceptionAsync(() => svc.SaveAsync(offer));
        Assert.Null(ex);

        using var verify = new AppDbContext(_options);
        var saved = await verify.Offers.Include(o => o.Items).FirstAsync(o => o.Id == offer.Id);
        Assert.Equal(2, saved.Items.Count);
    }

    public void Dispose() => _conn.Dispose();
}
