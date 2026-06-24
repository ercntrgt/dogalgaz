using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

/// <summary>
/// Tüm stok işlemleri (kontrol, düşme, rezervasyon, iade) burada yönetilir.
/// </summary>
public class StockControlService : IStockControlService
{
    private readonly AppDbContext _db;

    public StockControlService(AppDbContext db) => _db = db;

    // ---------------------------------------------------------------------
    // Stok durum mantığı (spec'teki C# bloğu ile birebir)
    // ---------------------------------------------------------------------
    private static void ApplyStockStatus(Product? product, decimal requested,
        out StockStatus status, out decimal available, out decimal missing, out bool insufficient)
    {
        if (product is null || !product.IsStockTracked)
        {
            status = StockStatus.NotTracked;
            available = product?.StockQuantity ?? 0;
            missing = 0;
            insufficient = false;
        }
        else if (product.StockQuantity < requested)
        {
            status = StockStatus.Insufficient;
            available = product.StockQuantity;
            missing = requested - product.StockQuantity;
            insufficient = true;
        }
        else if (product.StockQuantity - requested <= product.CriticalStockQuantity)
        {
            status = StockStatus.Critical;
            available = product.StockQuantity;
            missing = 0;
            insufficient = false;
        }
        else
        {
            status = StockStatus.Sufficient;
            available = product.StockQuantity;
            missing = 0;
            insufficient = false;
        }
    }

    public void CheckOfferItemStockAvailability(OfferItem item, Product? product)
    {
        var requested = item.RequestedQuantity > 0 ? item.RequestedQuantity : item.Quantity;
        // Seçili olmayan (D bölümü) kalemler için stok kontrolü yapılmaz.
        if (!item.IsSelected) { product = null; requested = 0; }

        ApplyStockStatus(product, requested,
            out var status, out var available, out var missing, out var insufficient);
        item.StockStatus = status;
        item.AvailableStock = available;
        item.MissingQuantity = missing;
        item.IsStockInsufficient = insufficient;
    }

    public void CheckRadiatorStockAvailability(RadiatorItem item, Product? radiatorProduct)
    {
        // Radyatör paneli (metre) ana stok kalemidir.
        ApplyStockStatus(radiatorProduct, item.PanelLength,
            out var status, out var available, out var missing, out var insufficient);
        item.StockStatus = status;
        item.AvailableStock = available;
        item.MissingQuantity = missing;
        item.IsStockInsufficient = insufficient;
    }

    public async Task<OfferStockSummary> CheckOfferStockAvailabilityAsync(Offer offer)
    {
        var summary = new OfferStockSummary();

        foreach (var item in offer.Items)
        {
            var product = item.ProductId.HasValue
                ? await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProductId)
                : null;
            CheckOfferItemStockAvailability(item, product);
            Tally(summary, item.StockStatus, item.ItemName, product?.Category,
                item.RequestedQuantity > 0 ? item.RequestedQuantity : item.Quantity,
                item.AvailableStock, item.MissingQuantity, item.Unit, item.Description);
        }

        foreach (var rad in offer.RadiatorItems)
        {
            var product = rad.RadiatorProductId.HasValue
                ? await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == rad.RadiatorProductId)
                : null;
            CheckRadiatorStockAvailability(rad, product);
            Tally(summary, rad.StockStatus, $"Radyatör - {rad.RoomName}", product?.Category,
                rad.PanelLength, rad.AvailableStock, rad.MissingQuantity, "metre", rad.Description);
        }

        // Genel uyarı metinleri
        if (summary.AllSufficient)
            summary.Messages.Add("Tüm ürünler için stok yeterlidir.");
        else
        {
            if (summary.InsufficientCount > 0)
                summary.Messages.Add($"Bu siparişte {summary.InsufficientCount} ürün için stok yetersiz.");
            if (summary.CriticalCount > 0)
                summary.Messages.Add($"{summary.CriticalCount} ürün kritik stok seviyesine düşecektir.");
        }

        return summary;
    }

    private static void Tally(OfferStockSummary summary, StockStatus status, string name,
        string? category, decimal requested, decimal available, decimal missing, string unit, string? desc)
    {
        if (status == StockStatus.Insufficient)
        {
            summary.InsufficientCount++;
            summary.InsufficientItems.Add(new InsufficientStockRow
            {
                ProductName = name,
                Category = category ?? "-",
                RequestedQuantity = requested,
                AvailableStock = available,
                MissingQuantity = missing,
                Unit = unit,
                Description = desc
            });
        }
        else if (status == StockStatus.Critical)
        {
            summary.CriticalCount++;
        }
    }

    // ---------------------------------------------------------------------
    // Stoktan düşme
    // ---------------------------------------------------------------------
    public async Task<StockDeductionResult> DeductStockForOfferAsync(int offerId)
    {
        var offer = await _db.Offers
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null)
            return new StockDeductionResult { Success = false, Message = "Teklif bulunamadı." };

        var settings = await GetSettingsAsync();
        var result = new StockDeductionResult { Success = true };

        foreach (var item in offer.Items)
        {
            if (!item.IsSelected || item.IsStockDeducted || !item.ProductId.HasValue) continue;
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product is null || !product.IsStockTracked) continue;

            var requested = item.RequestedQuantity > 0 ? item.RequestedQuantity : item.Quantity;
            await DeductOne(product, offer.Id, requested, item, null, settings, result);
        }

        foreach (var rad in offer.RadiatorItems)
        {
            if (rad.IsStockDeducted || !rad.RadiatorProductId.HasValue) continue;
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == rad.RadiatorProductId);
            if (product is null || !product.IsStockTracked) continue;

            await DeductOne(product, offer.Id, rad.PanelLength, null, rad, settings, result);
        }

        // Yetersiz kalem varsa sipariş "Bekleyen Tedarik" durumuna alınır.
        if (result.InsufficientItemCount > 0 && !settings.AllowNegativeStock)
        {
            offer.Status = OfferStatus.WaitingSupply;
            result.MovedToWaitingSupply = true;
        }

        await _db.SaveChangesAsync();

        result.Message = result.MovedToWaitingSupply
            ? $"{result.DeductedItemCount} kalem stoktan düşüldü. {result.InsufficientItemCount} kalem stok yetersiz → Bekleyen Tedarik."
            : $"{result.DeductedItemCount} kalem stoktan düşüldü.";
        return result;
    }

    private async Task DeductOne(Product product, int offerId, decimal requested,
        OfferItem? item, RadiatorItem? rad, StockSettings settings, StockDeductionResult result)
    {
        var sufficient = product.StockQuantity >= requested;
        if (!sufficient && !settings.AllowNegativeStock)
        {
            result.InsufficientItemCount++;
            return; // Stok eksiye düşürülmez.
        }

        var previous = product.StockQuantity;
        product.StockQuantity -= requested;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            OfferId = offerId,
            MovementType = MovementType.Out,
            Quantity = requested,
            PreviousStock = previous,
            NewStock = product.StockQuantity,
            Description = "Sipariş stok düşümü"
        });

        if (item is not null) item.IsStockDeducted = true;
        if (rad is not null) rad.IsStockDeducted = true;
        result.DeductedItemCount++;
    }

    public async Task ReserveStockForOfferAsync(int offerId)
    {
        var offer = await _db.Offers.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;

        foreach (var item in offer.Items.Where(i => i.IsSelected && i.ProductId.HasValue))
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product is null || !product.IsStockTracked) continue;
            var requested = item.RequestedQuantity > 0 ? item.RequestedQuantity : item.Quantity;
            product.ReservedQuantity += requested;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<InsufficientStockRow>> GetInsufficientStockItemsAsync(int offerId)
    {
        var offer = await _db.Offers
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return new();
        var summary = await CheckOfferStockAvailabilityAsync(offer);
        return summary.InsufficientItems;
    }

    public Task<List<Product>> GetCriticalStockItemsAsync() =>
        _db.Products.Where(p => p.IsStockTracked && p.IsActive
            && p.StockQuantity <= p.CriticalStockQuantity)
            .OrderBy(p => p.StockQuantity).ToListAsync();

    public async Task CreateStockMovementAsync(int productId, int? offerId,
        MovementType type, decimal quantity, string? note)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return;

        // Hareket anındaki güncel stok = NewStock. Önceki, türe göre türetilir.
        var newStock = product.StockQuantity;
        var previous = type is MovementType.In or MovementType.Restore
            ? newStock - quantity
            : newStock + quantity;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            OfferId = offerId,
            MovementType = type,
            Quantity = quantity,
            PreviousStock = previous,
            NewStock = newStock,
            Description = note
        });
        await _db.SaveChangesAsync();
    }

    public async Task RestoreStockForCancelledOrderAsync(int offerId)
    {
        var offer = await _db.Offers
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;

        foreach (var item in offer.Items.Where(i => i.IsStockDeducted && i.ProductId.HasValue))
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product is null) continue;
            var qty = item.RequestedQuantity > 0 ? item.RequestedQuantity : item.Quantity;
            var previous = product.StockQuantity;
            product.StockQuantity += qty;
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, OfferId = offerId, MovementType = MovementType.Restore,
                Quantity = qty, PreviousStock = previous, NewStock = product.StockQuantity,
                Description = "Sipariş iptali stok iadesi"
            });
            item.IsStockDeducted = false;
        }

        foreach (var rad in offer.RadiatorItems.Where(r => r.IsStockDeducted && r.RadiatorProductId.HasValue))
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == rad.RadiatorProductId);
            if (product is null) continue;
            var previous = product.StockQuantity;
            product.StockQuantity += rad.PanelLength;
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, OfferId = offerId, MovementType = MovementType.Restore,
                Quantity = rad.PanelLength, PreviousStock = previous, NewStock = product.StockQuantity,
                Description = "Sipariş iptali stok iadesi (radyatör)"
            });
            rad.IsStockDeducted = false;
        }

        await _db.SaveChangesAsync();
    }

    private async Task<StockSettings> GetSettingsAsync()
    {
        var settings = await _db.StockSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new StockSettings();
            _db.StockSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return settings;
    }
}
