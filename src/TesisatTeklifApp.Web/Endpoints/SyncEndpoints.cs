using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Web.Endpoints;

/// <summary>
/// Saha (MAUI) uygulamasının senkron uçları. Basit anahtar (X-Sync-Key) ile korunur;
/// ofis ağında çalışır. Offline teklifleri alır, katalog/müşteri verisi verir.
/// </summary>
public static class SyncEndpoints
{
    private const string SyncKey = "ozdemir-sync";   // basit ortak anahtar

    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g = endpoints.MapGroup("/api/sync");

        // Bağlantı testi
        g.MapGet("/ping", () => Results.Ok(new { ok = true, time = DateTime.Now }));

        // Katalog (offline'ın güncel ürünleri çekmesi için)
        g.MapGet("/catalog", (HttpContext ctx, AppDbContext db) =>
        {
            if (!KeyOk(ctx)) return Results.Unauthorized();
            var products = db.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.Name, p.Category, p.Brand, p.Model, p.Unit, p.PurchasePrice, p.SalePrice, p.VatRate })
                .ToList();
            return Results.Ok(products);
        });

        // Offline teklifleri al
        g.MapPost("/offers", async (HttpContext ctx, List<OfferSyncDto> offers,
            AppDbContext db, INumberGeneratorService numbers, IOfferCalculationService calc) =>
        {
            if (!KeyOk(ctx)) return Results.Unauthorized();

            var result = new SyncPushResult { Received = offers?.Count ?? 0 };
            if (offers is null) return Results.Ok(result);

            foreach (var dto in offers)
            {
                try
                {
                    // Müşteri: telefon ya da ad-soyad ile bul, yoksa oluştur.
                    var customer = await FindOrCreateCustomer(db, dto.Customer);

                    var offer = new Offer
                    {
                        CustomerId = customer.Id,
                        OfferNumber = await numbers.GenerateOfferNumberAsync(),
                        OfferDate = dto.OfferDate == default ? DateTime.Today : dto.OfferDate,
                        Status = (OfferStatus)dto.Status,
                        CreatedBy = dto.CreatedBy,
                        CustomerSignature = dto.CustomerSignature,
                        DiscountRate = dto.DiscountRate,
                        VatRate = dto.VatRate == 0 ? 20m : dto.VatRate,
                        IsVatIncluded = dto.IsVatIncluded,
                        GeneralNotes = string.IsNullOrEmpty(dto.OfferNumber)
                            ? dto.GeneralNotes
                            : $"[Saha No: {dto.OfferNumber}] {dto.GeneralNotes}".Trim(),
                        AdvancePayment = dto.AdvancePayment
                    };

                    // Sipariş ise sipariş no üret.
                    if (offer.Status is OfferStatus.ConvertedToOrder or OfferStatus.WaitingSupply or OfferStatus.Completed)
                        offer.OrderNumber = await numbers.GenerateOrderNumberAsync();

                    foreach (var i in dto.Items)
                    {
                        var prodId = await db.Products.Where(p => p.Name == i.ItemName)
                            .Select(p => (int?)p.Id).FirstOrDefaultAsync();
                        offer.Items.Add(new OfferItem
                        {
                            SectionName = i.SectionName, ItemName = i.ItemName, IsSelected = i.IsSelected,
                            Quantity = i.Quantity, Unit = i.Unit, UnitPrice = i.UnitPrice,
                            Description = i.Description, ProductId = prodId
                        });
                    }
                    foreach (var r in dto.Radiators)
                    {
                        offer.RadiatorItems.Add(new RadiatorItem
                        {
                            RoomName = r.RoomName, RadiatorBrand = r.RadiatorBrand,
                            RadiatorHeight = r.RadiatorHeight, RadiatorWidth = r.RadiatorWidth,
                            PanelLength = r.PanelLength, ValveQuantity = r.ValveQuantity,
                            MeterPrice = r.MeterPrice, ValveUnitPrice = r.ValveUnitPrice
                        });
                    }

                    calc.RecalculateOfferTotals(offer);
                    offer.AdvancePayment = dto.AdvancePayment;
                    offer.RemainingPayment = offer.GrandTotal - dto.AdvancePayment;

                    db.Offers.Add(offer);
                    await db.SaveChangesAsync();
                    result.Created++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{dto.Customer.FirstName}: {ex.Message}");
                }
            }

            return Results.Ok(result);
        });

        return endpoints;
    }

    private static bool KeyOk(HttpContext ctx) =>
        ctx.Request.Headers.TryGetValue("X-Sync-Key", out var k) && k == SyncKey;

    private static async Task<Customer> FindOrCreateCustomer(AppDbContext db, CustomerSyncDto c)
    {
        Customer? existing = null;
        if (!string.IsNullOrWhiteSpace(c.Phone))
            existing = await db.Customers.FirstOrDefaultAsync(x => x.Phone == c.Phone);
        existing ??= await db.Customers.FirstOrDefaultAsync(x =>
            x.FirstName == c.FirstName && x.LastName == (c.LastName ?? ""));

        if (existing is not null) return existing;

        var created = new Customer
        {
            FirstName = c.FirstName, LastName = c.LastName ?? "", Phone = c.Phone,
            NationalId = c.NationalId, City = c.City, District = c.District, Address = c.Address
        };
        db.Customers.Add(created);
        await db.SaveChangesAsync();
        return created;
    }
}
