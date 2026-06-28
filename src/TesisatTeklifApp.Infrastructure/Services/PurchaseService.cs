using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private static readonly SemaphoreSlim NumberGate = new(1, 1);
    private readonly AppDbContext _db;
    private readonly IStockControlService _stock;

    public PurchaseService(AppDbContext db, IStockControlService stock)
    {
        _db = db;
        _stock = stock;
    }

    public Task<List<PurchaseOrder>> SearchAsync(PurchaseStatus? status)
    {
        var q = _db.PurchaseOrders.Include(p => p.Supplier).AsQueryable();
        if (status.HasValue)
            q = q.Where(p => p.Status == status.Value);
        return q.OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id).ToListAsync();
    }

    public Task<PurchaseOrder?> GetByIdAsync(int id) =>
        _db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order)
    {
        // Navigasyonları temizle (yalnızca FK kullanılsın).
        order.Supplier = null;
        foreach (var it in order.Items)
        {
            it.Product = null;
            it.PurchaseOrder = null;
            it.TotalPrice = Math.Round(it.Quantity * it.UnitPrice, 2);
            if (it.ProductId is 0) it.ProductId = null;
        }
        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        order.PurchaseNumber = await GenerateNumberAsync();
        if (order.Status == PurchaseStatus.Draft)
            order.Status = PurchaseStatus.Ordered;

        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task UpdateStatusAsync(int id, PurchaseStatus status)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id);
        if (order is null) return;
        order.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task ReceiveAllAsync(int purchaseOrderId)
    {
        var order = await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
        if (order is null) return;

        foreach (var item in order.Items)
        {
            if (item.IsReceived || !item.ProductId.HasValue) continue;
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (product is null) continue;

            var remaining = item.Quantity - item.ReceivedQuantity;
            if (remaining <= 0) { item.IsReceived = true; continue; }

            var previous = product.StockQuantity;
            product.StockQuantity += remaining;

            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                MovementType = MovementType.In,
                Quantity = remaining,
                PreviousStock = previous,
                NewStock = product.StockQuantity,
                Description = $"Satınalma teslim ({order.PurchaseNumber})"
            });

            item.ReceivedQuantity = item.Quantity;
            item.IsReceived = true;
        }

        order.Status = PurchaseStatus.Received;
        await _db.SaveChangesAsync();
    }

    public async Task<List<PurchaseSuggestionRow>> GetSuggestionsAsync()
    {
        // 1) Kritik/min seviyenin altındaki stok takipli ürünler.
        var products = await _db.Products
            .Where(p => p.IsActive && p.IsStockTracked
                && p.StockQuantity <= (p.CriticalStockQuantity > p.MinimumStockQuantity ? p.CriticalStockQuantity : p.MinimumStockQuantity))
            .AsNoTracking()
            .ToListAsync();

        var rows = new Dictionary<int, PurchaseSuggestionRow>();
        foreach (var p in products)
        {
            var target = Math.Max(p.MinimumStockQuantity, p.CriticalStockQuantity) * 2;
            var suggested = Math.Max(target - p.StockQuantity, 1);
            rows[p.Id] = new PurchaseSuggestionRow
            {
                ProductId = p.Id, ProductName = p.Name, Category = p.Category, Unit = p.Unit,
                CurrentStock = p.StockQuantity, CriticalLevel = p.CriticalStockQuantity,
                MinimumLevel = p.MinimumStockQuantity, SuggestedQuantity = suggested,
                LastPurchasePrice = p.PurchasePrice, Reason = "Kritik/min stok altında"
            };
        }

        // 2) Bekleyen Tedarik siparişlerindeki eksik miktarlar.
        var waiting = await _db.Offers
            .Include(o => o.Items)
            .Where(o => o.Status == OfferStatus.WaitingSupply || o.Status == OfferStatus.ConvertedToOrder)
            .AsNoTracking().ToListAsync();

        foreach (var offer in waiting)
        {
            var summary = await _stock.CheckOfferStockAvailabilityAsync(offer);
            foreach (var item in offer.Items.Where(i => i.IsStockInsufficient && i.ProductId.HasValue))
            {
                var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ProductId);
                if (p is null) continue;
                if (rows.TryGetValue(p.Id, out var existing))
                {
                    existing.SuggestedQuantity += item.MissingQuantity;
                    existing.Reason = "Kritik stok + sipariş için eksik";
                }
                else
                {
                    rows[p.Id] = new PurchaseSuggestionRow
                    {
                        ProductId = p.Id, ProductName = p.Name, Category = p.Category, Unit = p.Unit,
                        CurrentStock = p.StockQuantity, CriticalLevel = p.CriticalStockQuantity,
                        MinimumLevel = p.MinimumStockQuantity, SuggestedQuantity = item.MissingQuantity,
                        LastPurchasePrice = p.PurchasePrice, Reason = "Sipariş için eksik"
                    };
                }
            }
        }

        return rows.Values.OrderBy(r => r.Category).ThenBy(r => r.ProductName).ToList();
    }

    private async Task<string> GenerateNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"SA-{year}-";
        await NumberGate.WaitAsync();
        try
        {
            var existing = await _db.PurchaseOrders.IgnoreQueryFilters()
                .Where(p => p.PurchaseNumber.StartsWith(prefix))
                .Select(p => p.PurchaseNumber).ToListAsync();
            var max = existing
                .Select(n => int.TryParse(n.Substring(prefix.Length), out var v) ? v : 0)
                .DefaultIfEmpty(0).Max();
            return $"{prefix}{(max + 1):D4}";
        }
        finally { NumberGate.Release(); }
    }
}
