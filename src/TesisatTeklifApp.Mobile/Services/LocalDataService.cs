using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Mobile.Data;

namespace TesisatTeklifApp.Mobile.Services;

/// <summary>Offline yerel veri işlemleri (ürün/müşteri/teklif). Sunucuya bağlı değildir.</summary>
public class LocalDataService
{
    private readonly LocalDbContext _db;
    private readonly IOfferCalculationService _calc;

    public LocalDataService(LocalDbContext db, IOfferCalculationService calc)
    {
        _db = db;
        _calc = calc;
    }

    private static bool _seeded;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    // Katalog seed'i ilk veri erişiminde (async, güvenli) bir kez çalışır.
    private async Task EnsureSeedAsync()
    {
        if (_seeded) return;
        await _gate.WaitAsync();
        try { if (!_seeded) { await LocalSeed.RunAsync(_db); _seeded = true; } }
        finally { _gate.Release(); }
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        await EnsureSeedAsync();
        return await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
    }

    public Task<List<Customer>> GetCustomersAsync() =>
        _db.Customers.OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToListAsync();

    public async Task AddCustomerAsync(Customer c)
    {
        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        _db.Outbox.Add(new OutboxEntry { EntityType = "Customer", LocalId = c.Id });
        await _db.SaveChangesAsync();
    }

    public Task<List<Offer>> GetOffersAsync() =>
        _db.Offers.Include(o => o.Customer).OrderByDescending(o => o.Id).ToListAsync();

    public Task<Offer?> GetOfferAsync(int id) =>
        _db.Offers
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task SaveOfferAsync(Offer offer)
    {
        _calc.RecalculateOfferTotals(offer);
        if (offer.Id == 0)
        {
            if (string.IsNullOrEmpty(offer.OfferNumber))
                offer.OfferNumber = $"TSF-OFL-{DateTime.Now:yyMMddHHmmss}";   // offline geçici no
            _db.Offers.Add(offer);
        }
        else
        {
            _db.Offers.Update(offer);
        }
        await _db.SaveChangesAsync();

        _db.Outbox.Add(new OutboxEntry { EntityType = "Offer", LocalId = offer.Id });
        await _db.SaveChangesAsync();
    }

    public Task<int> PendingCountAsync() => _db.Outbox.CountAsync(o => !o.Synced);
}
