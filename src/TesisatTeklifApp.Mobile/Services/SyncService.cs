using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Mobile.Data;

namespace TesisatTeklifApp.Mobile.Services;

/// <summary>
/// Senkron: internet gelince bekleyen teklifleri ofis sunucusuna gönderir (push).
/// </summary>
public class SyncService
{
    private readonly LocalDbContext _db;
    private readonly ConnectivityService _conn;
    private readonly HttpClient _http;

    public SyncService(LocalDbContext db, ConnectivityService conn, HttpClient http)
    {
        _db = db;
        _conn = conn;
        _http = http;
    }

    /// <summary>Ofis sunucu adresi (ayarlardan değiştirilebilir).</summary>
    public string ServerBaseUrl
    {
        get => Preferences.Get("serverUrl", "http://localhost:5219");
        set => Preferences.Set("serverUrl", value);
    }

    public Task<int> PendingCountAsync() => _db.Outbox.CountAsync(o => !o.Synced);

    // Manuel senkron her zaman dener (USB/adb-reverse'te ağ "yok" görünse de çalışsın).
    // Otomatik tetikleme yine bağlantı olunca yapılır.
    public async Task<SyncResult> SyncAsync()
    {
        var pendingEntries = await _db.Outbox.Where(o => !o.Synced).ToListAsync();
        var offerIds = pendingEntries.Where(e => e.EntityType == "Offer").Select(e => e.LocalId).Distinct().ToList();
        if (offerIds.Count == 0)
        {
            // Sadece müşteri kayıtları kalmışsa onları da senkron say.
            foreach (var e in pendingEntries) { e.Synced = true; e.SyncedAt = DateTime.UtcNow; }
            await _db.SaveChangesAsync();
            return new SyncResult { Success = true, Message = "Gönderilecek teklif yok." };
        }

        // Teklifleri DTO'ya çevir.
        var dtos = new List<OfferSyncDto>();
        foreach (var id in offerIds)
        {
            var o = await _db.Offers
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .Include(x => x.RadiatorItems)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) continue;

            dtos.Add(new OfferSyncDto
            {
                OfferNumber = o.OfferNumber,
                Status = (int)o.Status,
                OfferDate = o.OfferDate,
                CreatedBy = o.CreatedBy,
                CustomerSignature = o.CustomerSignature,
                DiscountRate = o.DiscountRate,
                VatRate = o.VatRate,
                IsVatIncluded = o.IsVatIncluded,
                GeneralNotes = o.GeneralNotes,
                AdvancePayment = o.AdvancePayment,
                Customer = new CustomerSyncDto
                {
                    FirstName = o.Customer?.FirstName ?? "Müşteri",
                    LastName = o.Customer?.LastName,
                    Phone = o.Customer?.Phone,
                    NationalId = o.Customer?.NationalId,
                    City = o.Customer?.City,
                    District = o.Customer?.District,
                    Address = o.Customer?.Address
                },
                Items = o.Items.Where(i => i.IsSelected && i.Quantity > 0).Select(i => new OfferItemSyncDto
                {
                    SectionName = i.SectionName, ItemName = i.ItemName, IsSelected = i.IsSelected,
                    Quantity = i.Quantity, Unit = i.Unit, UnitPrice = i.UnitPrice, Description = i.Description
                }).ToList(),
                Radiators = o.RadiatorItems.Where(r => r.PanelLength > 0 || r.ValveQuantity > 0).Select(r => new RadiatorSyncDto
                {
                    RoomName = r.RoomName, RadiatorBrand = r.RadiatorBrand,
                    RadiatorHeight = r.RadiatorHeight, RadiatorWidth = r.RadiatorWidth,
                    PanelLength = r.PanelLength, ValveQuantity = r.ValveQuantity,
                    MeterPrice = r.MeterPrice, ValveUnitPrice = r.ValveUnitPrice
                }).ToList()
            });
        }

        try
        {
            var url = ServerBaseUrl.TrimEnd('/') + "/api/sync/offers";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("X-Sync-Key", "ozdemir-sync");
            req.Content = JsonContent.Create(dtos);
            using var resp = await _http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                return new SyncResult { Success = false, Message = $"Sunucu hatası: {(int)resp.StatusCode}. Adres/anahtar doğru mu?" };

            var pushed = await resp.Content.ReadFromJsonAsync<SyncPushResult>();

            // Başarılı → bekleyenleri synced işaretle.
            foreach (var e in pendingEntries) { e.Synced = true; e.SyncedAt = DateTime.UtcNow; }
            await _db.SaveChangesAsync();

            return new SyncResult
            {
                Success = true,
                Pending = 0,
                Message = $"{pushed?.Created ?? dtos.Count} kayıt sunucuya iletildi."
            };
        }
        catch (Exception ex)
        {
            return new SyncResult { Success = false, Message = $"Bağlantı hatası: {ex.Message}" };
        }
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public int Pending { get; set; }
    public string Message { get; set; } = "";
}
