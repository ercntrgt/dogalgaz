using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class OfferService : IOfferService
{
    private readonly AppDbContext _db;
    private readonly IOfferCalculationService _calc;
    private readonly INumberGeneratorService _numbers;
    private readonly IStockControlService _stock;

    public OfferService(AppDbContext db, IOfferCalculationService calc,
        INumberGeneratorService numbers, IStockControlService stock)
    {
        _db = db;
        _calc = calc;
        _numbers = numbers;
        _stock = stock;
    }

    public async Task<List<Offer>> SearchAsync(OfferSearchFilter filter)
    {
        var q = _db.Offers.Include(o => o.Customer).AsQueryable();

        if (filter.OnlyOrders)
            q = q.Where(o => o.Status == OfferStatus.ConvertedToOrder
                || o.Status == OfferStatus.WaitingSupply || o.Status == OfferStatus.Completed);
        if (filter.FromDate.HasValue)
            q = q.Where(o => o.OfferDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            q = q.Where(o => o.OfferDate <= filter.ToDate.Value.Date);
        if (filter.Status.HasValue)
            q = q.Where(o => o.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.FormNumber))
            q = q.Where(o => o.OfferNumber.Contains(filter.FormNumber)
                || (o.OrderNumber != null && o.OrderNumber.Contains(filter.FormNumber)));
        if (!string.IsNullOrWhiteSpace(filter.ResponsiblePerson))
            q = q.Where(o => o.ResponsiblePerson != null && o.ResponsiblePerson.Contains(filter.ResponsiblePerson));
        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            q = q.Where(o => o.Customer != null &&
                (o.Customer.FirstName.Contains(filter.CustomerName) || o.Customer.LastName.Contains(filter.CustomerName)));

        return await q.OrderByDescending(o => o.OfferDate).ThenByDescending(o => o.Id).ToListAsync();
    }

    public Task<Offer?> GetByIdAsync(int id) =>
        _db.Offers
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Offer> CreateDraftAsync(int customerId, string? responsiblePerson)
    {
        var offer = new Offer
        {
            CustomerId = customerId,
            OfferNumber = await _numbers.GenerateOfferNumberAsync(),
            OfferDate = DateTime.Today,
            Status = OfferStatus.Draft,
            ResponsiblePerson = responsiblePerson,
            VatRate = 20m
        };
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync();
        return offer;
    }

    /// <summary>
    /// Teklifi kaydeder. Yeni ise ekler; mevcut ise alt satırları tam değiştirir
    /// (detached graph ile çağrıldığı için orphan satır kalmaz).
    /// </summary>
    public async Task SaveAsync(Offer offer)
    {
        _calc.RecalculateOfferTotals(offer);

        if (offer.Id == 0)
        {
            if (string.IsNullOrEmpty(offer.OfferNumber))
                offer.OfferNumber = await _numbers.GenerateOfferNumberAsync();
            _db.Offers.Add(offer);
            await _db.SaveChangesAsync();
            return;
        }

        // Mevcut kayıt: taze yükle, eski alt satırları sil, yenilerini ekle.
        var existing = await _db.Offers
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .FirstOrDefaultAsync(o => o.Id == offer.Id)
            ?? throw new InvalidOperationException("Teklif bulunamadı.");

        _db.OfferItems.RemoveRange(existing.Items);
        _db.RadiatorItems.RemoveRange(existing.RadiatorItems);
        _db.PaymentPlans.RemoveRange(existing.PaymentPlans);

        // Skaler alanları kopyala (numara/oluşturma tarihi korunur).
        var number = existing.OfferNumber;
        var orderNumber = existing.OrderNumber;
        var created = existing.CreatedDate;
        _db.Entry(existing).CurrentValues.SetValues(offer);
        existing.OfferNumber = number;
        existing.OrderNumber = orderNumber;
        existing.CreatedDate = created;

        foreach (var it in offer.Items)
        {
            it.Id = 0; it.OfferId = existing.Id; existing.Items.Add(it);
        }
        foreach (var r in offer.RadiatorItems)
        {
            r.Id = 0; r.OfferId = existing.Id; existing.RadiatorItems.Add(r);
        }
        foreach (var p in offer.PaymentPlans)
        {
            p.Id = 0; p.OfferId = existing.Id; existing.PaymentPlans.Add(p);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<Offer> CopyAsync(int offerId)
    {
        var source = await GetByIdAsync(offerId)
            ?? throw new InvalidOperationException("Kopyalanacak teklif bulunamadı.");

        var copy = new Offer
        {
            CustomerId = source.CustomerId,
            OfferNumber = await _numbers.GenerateOfferNumberAsync(),
            OfferDate = DateTime.Today,
            Status = OfferStatus.Draft,
            VatRate = source.VatRate,
            DiscountRate = source.DiscountRate,
            IsVatIncluded = source.IsVatIncluded,
            GeneralNotes = source.GeneralNotes,
            ResponsiblePerson = source.ResponsiblePerson,
            EmployerName = source.EmployerName,
            Items = source.Items.Select(i => new OfferItem
            {
                ProductId = i.ProductId, SectionName = i.SectionName, ItemName = i.ItemName,
                IsSelected = i.IsSelected, Quantity = i.Quantity, Unit = i.Unit,
                UnitPrice = i.UnitPrice, Description = i.Description
            }).ToList(),
            RadiatorItems = source.RadiatorItems.Select(r => new RadiatorItem
            {
                RadiatorProductId = r.RadiatorProductId, ValveProductId = r.ValveProductId,
                RoomName = r.RoomName, RadiatorBrand = r.RadiatorBrand, RadiatorSize = r.RadiatorSize,
                PanelLength = r.PanelLength, ValveQuantity = r.ValveQuantity,
                MeterPrice = r.MeterPrice, ValveUnitPrice = r.ValveUnitPrice, Description = r.Description
            }).ToList()
        };

        _calc.RecalculateOfferTotals(copy);
        _db.Offers.Add(copy);
        await _db.SaveChangesAsync();
        return copy;
    }

    public async Task ChangeStatusAsync(int offerId, OfferStatus status)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;
        offer.Status = status;
        await _db.SaveChangesAsync();

        // Onay anında stoktan düşme ayarı aktifse stok düş.
        if (status == OfferStatus.Approved)
        {
            var settings = await _db.StockSettings.FirstOrDefaultAsync();
            if (settings?.StockDeductionMode == StockDeductionMode.DeductOnApproval)
                await _stock.DeductStockForOfferAsync(offerId);
        }
        else if (status == OfferStatus.Completed)
        {
            var settings = await _db.StockSettings.FirstOrDefaultAsync();
            if (settings?.StockDeductionMode == StockDeductionMode.DeductOnCompletion)
                await _stock.DeductStockForOfferAsync(offerId);
        }
    }

    public async Task<Offer> ConvertToOrderAsync(int offerId)
    {
        var offer = await _db.Offers
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new InvalidOperationException("Teklif bulunamadı.");

        // Sipariş numarası üret (yoksa) ve durumu güncelle.
        if (string.IsNullOrEmpty(offer.OrderNumber))
            offer.OrderNumber = await _numbers.GenerateOrderNumberAsync();
        offer.Status = OfferStatus.ConvertedToOrder;

        // RequestedQuantity'leri kilitle (talep edilen miktar = mevcut adet/metre).
        foreach (var item in offer.Items)
            item.RequestedQuantity = item.Quantity;
        await _db.SaveChangesAsync();

        // Manuel mod hariç stoktan düş. (Manuel modda stok durumu yine de
        // detay ekranında ve PDF'te kontrol edilip kırmızı gösterilir.)
        var settings = await _db.StockSettings.FirstOrDefaultAsync();
        if (settings?.StockDeductionMode != StockDeductionMode.Manual)
            await _stock.DeductStockForOfferAsync(offerId);

        return (await GetByIdAsync(offerId))!;
    }

    public async Task CancelAsync(int offerId)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;

        // Daha önce düşülen stok geri eklenir.
        await _stock.RestoreStockForCancelledOrderAsync(offerId);

        offer.Status = OfferStatus.Cancelled;
        await _db.SaveChangesAsync();
    }
}
