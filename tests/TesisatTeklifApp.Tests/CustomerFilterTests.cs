using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class CustomerFilterTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public CustomerFilterTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        // Eski taslak + yeni teslim edilmiş iş → son durum "Tamamlandı"
        var delivered = new Customer { FirstName = "Teslim", LastName = "Edilen" };
        delivered.Offers.Add(new Offer { OfferNumber = "T1", Status = OfferStatus.Draft, CreatedDate = new DateTime(2026, 1, 1) });
        delivered.Offers.Add(new Offer { OfferNumber = "T2", Status = OfferStatus.Completed, CreatedDate = new DateTime(2026, 2, 1) });

        var draft = new Customer { FirstName = "Taslak", LastName = "Olan" };
        draft.Offers.Add(new Offer { OfferNumber = "T3", Status = OfferStatus.Draft, CreatedDate = new DateTime(2026, 3, 1) });

        var none = new Customer { FirstName = "Teklifsiz", LastName = "Müşteri" };

        ctx.Customers.AddRange(delivered, draft, none);
        ctx.SaveChanges();
    }

    private async Task<List<string>> Names(CustomerSearchFilter f)
    {
        using var ctx = new AppDbContext(_options);
        return (await new CustomerService(ctx).SearchAsync(f)).Select(c => c.FirstName).ToList();
    }

    [Fact]
    public async Task Filters_By_Latest_Offer_Status()
    {
        Assert.Equal(new[] { "TASLAK" }, await Names(new() { LastOfferStatus = OfferStatus.Draft }));
        Assert.Equal(new[] { "TESLİM" }, await Names(new() { LastOfferStatus = OfferStatus.Completed }));
        Assert.Equal(new[] { "TEKLİFSİZ" }, await Names(new() { OnlyWithoutOffer = true }));
        Assert.Equal(3, (await Names(new())).Count);
    }

    [Fact]
    public async Task Latest_Status_Map_Matches_Filter()
    {
        using var ctx = new AppDbContext(_options);
        var svc = new CustomerService(ctx);
        var all = await svc.SearchAsync(new());
        var map = await svc.GetLatestOfferStatusesAsync(all.Select(c => c.Id));

        Assert.Equal(2, map.Count);   // teklifsiz müşteri sözlükte yok
        Assert.Equal(OfferStatus.Completed, map[all.Single(c => c.FirstName == "TESLİM").Id]);
        Assert.Equal(OfferStatus.Draft, map[all.Single(c => c.FirstName == "TASLAK").Id]);
    }

    public void Dispose() => _conn.Dispose();
}
