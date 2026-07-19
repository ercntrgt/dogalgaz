using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

/// <summary>
/// Sistemi teslim/devreye alma öncesi hareket verilerini temizler.
/// Ürünler, kullanıcılar ve ayarlar KORUNUR. Geri alınamaz.
/// </summary>
public class DataResetService : IDataResetService
{
    private readonly AppDbContext _db;

    public DataResetService(AppDbContext db) => _db = db;

    public async Task<DataResetCounts> GetCountsAsync() => new()
    {
        Customers = await _db.Customers.IgnoreQueryFilters().CountAsync(),
        Offers = await _db.Offers.IgnoreQueryFilters().CountAsync(),
        PaymentPlans = await _db.PaymentPlans.IgnoreQueryFilters().CountAsync(),
        ServiceRecords = await _db.ServiceRecords.IgnoreQueryFilters().CountAsync(),
        Ustalar = await _db.Ustalar.IgnoreQueryFilters().CountAsync(),
        UstaPayments = await _db.UstaPayments.IgnoreQueryFilters().CountAsync(),
        PurchaseOrders = await _db.PurchaseOrders.IgnoreQueryFilters().CountAsync(),
        Suppliers = await _db.Suppliers.IgnoreQueryFilters().CountAsync(),
        StockMovements = await _db.StockMovements.IgnoreQueryFilters().CountAsync(),
        ActivityLogs = await _db.ActivityLogs.IgnoreQueryFilters().CountAsync(),
        Products = await _db.Products.IgnoreQueryFilters().CountAsync(),
    };

    /// <summary>
    /// Hareket verilerini kalıcı olarak siler (soft-delete değil). Silme sırası
    /// yabancı anahtar kısıtlarına göredir: önce çocuk kayıtlar, sonra ana kayıtlar.
    /// </summary>
    public async Task<DataResetCounts> PurgeAsync(bool zeroStock)
    {
        var before = await GetCountsAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1) Teklif/sipariş alt kayıtları
        await _db.PaymentPlans.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.OfferItems.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.RadiatorItems.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.OfferPhotos.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 2) Ürüne/teklife bağlı hareketler ve loglar
        await _db.StockMovements.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.ActivityLogs.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 3) Hakediş → usta bağı; satınalma alt kalemleri
        await _db.UstaPayments.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.PurchaseOrderItems.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.PurchaseOrders.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 4) Servis kayıtları (müşteriye Restrict ile bağlı → müşteriden önce)
        await _db.ServiceRecords.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 5) Teklifler (müşteri ve ustaya Restrict ile bağlı → onlardan önce)
        await _db.Offers.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 6) Ana kayıtlar
        await _db.Ustalar.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.Suppliers.IgnoreQueryFilters().ExecuteDeleteAsync();
        await _db.Customers.IgnoreQueryFilters().ExecuteDeleteAsync();

        // 7) Ürünler kalır; stok/rezerve alanları sıfırlanır.
        if (zeroStock)
            await _db.Products.IgnoreQueryFilters()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, 0m)
                    .SetProperty(p => p.ReservedQuantity, 0m));
        else
            await _db.Products.IgnoreQueryFilters()
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReservedQuantity, 0m));

        await tx.CommitAsync();

        return before;
    }
}
