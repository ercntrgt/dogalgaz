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
        if (filter.ExcludeOrders)
            q = q.Where(o => o.Status != OfferStatus.ConvertedToOrder
                && o.Status != OfferStatus.WaitingSupply && o.Status != OfferStatus.Completed);
        if (!string.IsNullOrEmpty(filter.CreatedBy))
            q = q.Where(o => o.CreatedBy == filter.CreatedBy);
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

    public Task<List<Offer>> GetByCustomerAsync(int customerId) =>
        _db.Offers
            .AsNoTracking()
            .Include(o => o.PaymentPlans)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OfferDate).ThenByDescending(o => o.Id)
            .ToListAsync();

    public async Task<List<Offer>> GetWorkScheduleAsync(DateTime? from, DateTime? to)
    {
        // Onaylı teklifler ve tüm siparişler (iptal/taslak hariç) takvime düşer.
        // İş başlangıç tarihi girilmişse o, yoksa teklif/sipariş tarihi kullanılır.
        var candidates = await _db.Offers
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Usta)          // takvimde işe gidecek usta gösterilir
            .Where(o => o.Status == OfferStatus.Approved
                || o.Status == OfferStatus.ConvertedToOrder
                || o.Status == OfferStatus.WaitingSupply
                || o.Status == OfferStatus.Completed
                || o.WorkStartDate != null)
            .ToListAsync();

        IEnumerable<Offer> q = candidates;
        if (from.HasValue)
            q = q.Where(o => (o.WorkStartDate ?? o.OfferDate).Date >= from.Value.Date);
        if (to.HasValue)
            q = q.Where(o => (o.WorkStartDate ?? o.OfferDate).Date <= to.Value.Date);

        return q.OrderBy(o => o.WorkStartDate ?? o.OfferDate).ToList();
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

        // Navigasyonları temizle: EF yalnızca FK (ProductId/CustomerId) kullansın.
        // Aksi halde aynı ürün birden çok satırda olunca "already being tracked" çakışması olur.
        DetachNavigations(offer);

        // Blazor Server'da scoped DbContext oturum boyunca yaşar; önceki sorgular
        // (ürün/müşteri dropdown'ları) ürünleri izliyor olabilir. Yazımdan önce
        // izleyiciyi temizleyerek çift-izleme çakışmasını kesin olarak önle.
        _db.ChangeTracker.Clear();

        if (offer.Id == 0)
        {
            if (string.IsNullOrEmpty(offer.OfferNumber))
                offer.OfferNumber = await _numbers.GenerateOfferNumberAsync();
            if (string.IsNullOrEmpty(offer.PublicToken))
                offer.PublicToken = GenToken();
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

        // Skaler alanları kopyala (numara/oluşturma/imza/token bilgisi korunur).
        var number = existing.OfferNumber;
        var orderNumber = existing.OrderNumber;
        var created = existing.CreatedDate;
        var createdBy = existing.CreatedBy;
        var token = existing.PublicToken;
        var signature = existing.CustomerSignature;
        var approvedDate = existing.CustomerApprovedDate;
        _db.Entry(existing).CurrentValues.SetValues(offer);
        existing.OfferNumber = number;
        existing.OrderNumber = orderNumber;
        existing.CreatedDate = created;
        existing.CreatedBy = createdBy;
        existing.PublicToken = token ?? GenToken();
        existing.CustomerSignature = signature;
        existing.CustomerApprovedDate = approvedDate;

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

    /// <summary>
    /// Tüm navigasyon referanslarını null'lar; EF yalnızca FK alanlarını kullanır.
    /// Geçersiz ProductId (0) değerlerini de null'a çevirir (FK ihlalini önler).
    /// </summary>
    private static void DetachNavigations(Offer offer)
    {
        offer.Customer = null;
        foreach (var it in offer.Items)
        {
            it.Product = null;
            it.Offer = null;
            if (it.ProductId is 0) it.ProductId = null;
        }
        foreach (var r in offer.RadiatorItems)
        {
            r.RadiatorProduct = null;
            r.ValveProduct = null;
            r.Offer = null;
            if (r.RadiatorProductId is 0) r.RadiatorProductId = null;
            if (r.ValveProductId is 0) r.ValveProductId = null;
        }
        foreach (var p in offer.PaymentPlans)
            p.Offer = null;
    }

    public async Task<Offer> CopyAsync(int offerId, string? createdBy = null)
    {
        var source = await GetByIdAsync(offerId)
            ?? throw new InvalidOperationException("Kopyalanacak teklif bulunamadı.");

        var copy = new Offer
        {
            CustomerId = source.CustomerId,
            OfferNumber = await _numbers.GenerateOfferNumberAsync(),
            OfferDate = DateTime.Today,
            Status = OfferStatus.Draft,
            CreatedBy = createdBy ?? source.CreatedBy,
            VatRate = source.VatRate,
            DiscountRate = source.DiscountRate,
            IsVatIncluded = source.IsVatIncluded,
            PaymentMethod = source.PaymentMethod,
            GeneralNotes = source.GeneralNotes,
            ResponsiblePerson = source.ResponsiblePerson,
            EmployerName = source.EmployerName,
            Items = source.Items.Select(i => new OfferItem
            {
                ProductId = i.ProductId, SectionName = i.SectionName, GroupName = i.GroupName,
                ItemName = i.ItemName, IsSelected = i.IsSelected, Quantity = i.Quantity,
                Unit = i.Unit, UnitPrice = i.UnitPrice, Description = i.Description
            }).ToList(),
            RadiatorItems = source.RadiatorItems.Select(r => new RadiatorItem
            {
                IsValve = r.IsValve, ItemName = r.ItemName, Quantity = r.Quantity, UnitPrice = r.UnitPrice,
                RadiatorProductId = r.RadiatorProductId, ValveProductId = r.ValveProductId,
                RoomName = r.RoomName, RadiatorBrand = r.RadiatorBrand, RadiatorSize = r.RadiatorSize,
                RadiatorHeight = r.RadiatorHeight, RadiatorWidth = r.RadiatorWidth,
                PanelLength = r.PanelLength, Description = r.Description
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

    public async Task DeleteAsync(int offerId)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;
        // Sipariş olup stok düşülmüşse iade et.
        if (offer.IsOrder)
            await _stock.RestoreStockForCancelledOrderAsync(offerId);
        offer.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task RequestDeleteAsync(int offerId, string user)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;
        offer.DeleteRequested = true;
        offer.DeleteRequestedBy = user;
        await _db.SaveChangesAsync();
    }

    public async Task RejectDeleteAsync(int offerId)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;
        offer.DeleteRequested = false;
        offer.DeleteRequestedBy = null;
        await _db.SaveChangesAsync();
    }

    public async Task SaveSignatureAsync(int offerId, string? signatureBase64)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;
        offer.CustomerSignature = signatureBase64;
        await _db.SaveChangesAsync();
    }

    // ===== Müşteri özel onay linki (anonim, token ile) =====
    public Task<Offer?> GetByPublicTokenAsync(string token) =>
        _db.Offers
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.PublicToken == token);

    public async Task<bool> ApproveByTokenAsync(string token, string signatureBase64)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.PublicToken == token);
        if (offer is null) return false;
        // Zaten onaylıysa tekrar imzalatmaz.
        if (offer.CustomerApprovedDate is not null) return true;
        offer.CustomerSignature = signatureBase64;
        offer.CustomerApprovedDate = DateTime.Now;
        if (offer.Status is OfferStatus.Draft or OfferStatus.SentToCustomer)
            offer.Status = OfferStatus.Approved;
        await _db.SaveChangesAsync();
        return true;
    }

    private static string GenToken() =>
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..8];

    public async Task MarkDeliveredAsync(int offerId, string? userName)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null) return;

        // Tamamlanınca stoktan düşme ayarı aktifse, henüz düşülmemiş kalemleri düş.
        var settings = await _db.StockSettings.FirstOrDefaultAsync();
        if (settings?.StockDeductionMode == StockDeductionMode.DeductOnCompletion)
            await _stock.DeductStockForOfferAsync(offerId);

        offer.Status = OfferStatus.Completed;
        offer.DeliveredDate = DateTime.Today;
        offer.DeliveredBy = userName;
        await _db.SaveChangesAsync();
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
