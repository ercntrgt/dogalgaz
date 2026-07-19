using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class StockStatusLogicTests
{
    private readonly StockControlService _svc = new(null!); // saf metot _db kullanmaz

    private static Product Tracked(decimal stock, decimal critical) =>
        new() { Id = 1, IsStockTracked = true, StockQuantity = stock, CriticalStockQuantity = critical };

    [Fact]
    public void NotTracked_When_Product_Not_Stock_Tracked()
    {
        var item = new OfferItem { Quantity = 5, RequestedQuantity = 5, IsSelected = true };
        _svc.CheckOfferItemStockAvailability(item, new Product { IsStockTracked = false });
        Assert.Equal(StockStatus.NotTracked, item.StockStatus);
        Assert.False(item.IsStockInsufficient);
    }

    [Fact]
    public void Insufficient_When_Stock_Below_Requested()
    {
        var item = new OfferItem { Quantity = 10, RequestedQuantity = 10, IsSelected = true };
        _svc.CheckOfferItemStockAvailability(item, Tracked(stock: 6, critical: 2));
        Assert.Equal(StockStatus.Insufficient, item.StockStatus);
        Assert.True(item.IsStockInsufficient);
        Assert.Equal(4m, item.MissingQuantity);   // 10 - 6
        Assert.Equal(6m, item.AvailableStock);
    }

    [Fact]
    public void Critical_When_Remaining_At_Or_Below_Critical_Level()
    {
        var item = new OfferItem { Quantity = 8, RequestedQuantity = 8, IsSelected = true };
        // 10 - 8 = 2 <= critical(2) -> Critical
        _svc.CheckOfferItemStockAvailability(item, Tracked(stock: 10, critical: 2));
        Assert.Equal(StockStatus.Critical, item.StockStatus);
        Assert.False(item.IsStockInsufficient);
        Assert.Equal(0m, item.MissingQuantity);
    }

    [Fact]
    public void Sufficient_When_Plenty_Of_Stock()
    {
        var item = new OfferItem { Quantity = 2, RequestedQuantity = 2, IsSelected = true };
        _svc.CheckOfferItemStockAvailability(item, Tracked(stock: 100, critical: 10));
        Assert.Equal(StockStatus.Sufficient, item.StockStatus);
        Assert.False(item.IsStockInsufficient);
    }
}

public class StockDeductionIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly int _customerId;

    public StockDeductionIntegrationTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.StockSettings.Add(new StockSettings { AllowNegativeStock = false });
        var customer = new Customer { FirstName = "Test", LastName = "Müşteri" };
        ctx.Customers.Add(customer);
        ctx.SaveChanges();
        _customerId = customer.Id;
    }

    private AppDbContext NewCtx() => new(_options);

    [Fact]
    public async Task Deduct_Reduces_Stock_For_Sufficient_And_Skips_Insufficient()
    {
        int offerId, p1Id, p2Id;
        using (var ctx = NewCtx())
        {
            var p1 = new Product { Name = "Yeterli", IsStockTracked = true, StockQuantity = 50, SalePrice = 10 };
            var p2 = new Product { Name = "Yetersiz", IsStockTracked = true, StockQuantity = 3, SalePrice = 10 };
            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var offer = new Offer { OfferNumber = "TSF-2026-0001", CustomerId = _customerId, Status = OfferStatus.ConvertedToOrder };
            offer.Items.Add(new OfferItem { ProductId = p1.Id, Quantity = 5, RequestedQuantity = 5, IsSelected = true });
            offer.Items.Add(new OfferItem { ProductId = p2.Id, Quantity = 10, RequestedQuantity = 10, IsSelected = true });
            ctx.Offers.Add(offer);
            await ctx.SaveChangesAsync();
            offerId = offer.Id; p1Id = p1.Id; p2Id = p2.Id;
        }

        using (var ctx = NewCtx())
        {
            var svc = new StockControlService(ctx);
            var result = await svc.DeductStockForOfferAsync(offerId);
            Assert.Equal(1, result.DeductedItemCount);
            Assert.Equal(1, result.InsufficientItemCount);
            Assert.True(result.MovedToWaitingSupply);
        }

        using (var ctx = NewCtx())
        {
            var sufficient = await ctx.Products.FirstAsync(p => p.Id == p1Id);
            var insufficient = await ctx.Products.FirstAsync(p => p.Id == p2Id);
            Assert.Equal(45m, sufficient.StockQuantity);   // 50 - 5
            Assert.Equal(3m, insufficient.StockQuantity);  // değişmedi (eksiye düşmez)

            var offer = await ctx.Offers.FirstAsync(o => o.Id == offerId);
            Assert.Equal(OfferStatus.WaitingSupply, offer.Status);
        }
    }

    [Fact]
    public async Task Deduct_Is_Idempotent_No_Double_Deduction()
    {
        int offerId, prodId;
        using (var ctx = NewCtx())
        {
            var p = new Product { Name = "Tekil", IsStockTracked = true, StockQuantity = 20, SalePrice = 10 };
            ctx.Products.Add(p);
            await ctx.SaveChangesAsync();
            var offer = new Offer { OfferNumber = "TSF-2026-0002", CustomerId = _customerId, Status = OfferStatus.ConvertedToOrder };
            offer.Items.Add(new OfferItem { ProductId = p.Id, Quantity = 5, RequestedQuantity = 5, IsSelected = true });
            ctx.Offers.Add(offer);
            await ctx.SaveChangesAsync();
            offerId = offer.Id; prodId = p.Id;
        }

        using (var ctx = NewCtx()) await new StockControlService(ctx).DeductStockForOfferAsync(offerId);
        using (var ctx = NewCtx()) await new StockControlService(ctx).DeductStockForOfferAsync(offerId); // ikinci kez

        using (var ctx = NewCtx())
        {
            var p = await ctx.Products.FirstAsync(x => x.Id == prodId);
            Assert.Equal(15m, p.StockQuantity);  // sadece bir kez düşülmüş (20 - 5)
        }
    }

    [Fact]
    public async Task Restore_Adds_Back_Deducted_Stock_On_Cancel()
    {
        int offerId, prodId;
        using (var ctx = NewCtx())
        {
            var p = new Product { Name = "İade", IsStockTracked = true, StockQuantity = 30, SalePrice = 10 };
            ctx.Products.Add(p);
            await ctx.SaveChangesAsync();
            var offer = new Offer { OfferNumber = "TSF-2026-0003", CustomerId = _customerId, Status = OfferStatus.ConvertedToOrder };
            offer.Items.Add(new OfferItem { ProductId = p.Id, Quantity = 8, RequestedQuantity = 8, IsSelected = true });
            ctx.Offers.Add(offer);
            await ctx.SaveChangesAsync();
            offerId = offer.Id; prodId = p.Id;
        }

        using (var ctx = NewCtx()) await new StockControlService(ctx).DeductStockForOfferAsync(offerId);
        using (var ctx = NewCtx())
        {
            var p = await ctx.Products.FirstAsync(x => x.Id == prodId);
            Assert.Equal(22m, p.StockQuantity); // 30 - 8
        }

        using (var ctx = NewCtx()) await new StockControlService(ctx).RestoreStockForCancelledOrderAsync(offerId);
        using (var ctx = NewCtx())
        {
            var p = await ctx.Products.FirstAsync(x => x.Id == prodId);
            Assert.Equal(30m, p.StockQuantity); // geri eklendi
        }
    }

    public void Dispose() => _conn.Dispose();
}
