using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly IStockControlService _stock;

    public ReportService(AppDbContext db, IStockControlService stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<List<Offer>> GetOffersAsync(OfferSearchFilter filter)
    {
        var q = _db.Offers.Include(o => o.Customer).AsQueryable();
        if (filter.OnlyOrders)
            q = q.Where(o => o.Status == OfferStatus.ConvertedToOrder
                || o.Status == OfferStatus.WaitingSupply || o.Status == OfferStatus.Completed);
        if (filter.Status.HasValue)
            q = q.Where(o => o.Status == filter.Status.Value);
        if (filter.FromDate.HasValue)
            q = q.Where(o => o.OfferDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            q = q.Where(o => o.OfferDate <= filter.ToDate.Value.Date);
        return await q.OrderByDescending(o => o.OfferDate).ToListAsync();
    }

    public async Task<List<InsufficientStockRow>> GetInsufficientStockReportAsync(ReportFilter filter)
    {
        // Bekleyen tedarik / sipariş durumundaki tekliflerin eksik kalemlerini topla.
        var orders = await _db.Offers
            .Include(o => o.Items).Include(o => o.RadiatorItems)
            .Where(o => o.Status == OfferStatus.ConvertedToOrder
                || o.Status == OfferStatus.WaitingSupply
                || o.Status == OfferStatus.Approved)
            .Where(o => !filter.FromDate.HasValue || o.OfferDate >= filter.FromDate.Value.Date)
            .Where(o => !filter.ToDate.HasValue || o.OfferDate <= filter.ToDate.Value.Date)
            .AsNoTracking()
            .ToListAsync();

        var rows = new List<InsufficientStockRow>();
        foreach (var o in orders)
        {
            var summary = await _stock.CheckOfferStockAvailabilityAsync(o);
            rows.AddRange(summary.InsufficientItems);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            rows = rows.Where(r => r.Category == filter.Category).ToList();

        return rows.OrderByDescending(r => r.MissingQuantity).ToList();
    }

    public async Task<List<Product>> GetCriticalStockReportAsync(ReportFilter filter)
    {
        var list = await _stock.GetCriticalStockItemsAsync();
        if (!string.IsNullOrWhiteSpace(filter.Category))
            list = list.Where(p => p.Category == filter.Category).ToList();
        return list;
    }

    public async Task<List<RevenueReportRow>> GetRevenueReportAsync(ReportFilter filter)
    {
        var q = _db.Offers.Where(o => o.Status == OfferStatus.Completed
            || o.Status == OfferStatus.ConvertedToOrder || o.Status == OfferStatus.WaitingSupply);
        if (filter.FromDate.HasValue) q = q.Where(o => o.OfferDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) q = q.Where(o => o.OfferDate <= filter.ToDate.Value.Date);

        var data = await q.ToListAsync();
        return data
            .GroupBy(o => o.OfferDate.ToString("yyyy-MM"))
            .Select(g => new RevenueReportRow
            {
                Period = g.Key,
                OrderCount = g.Count(),
                TotalRevenue = g.Sum(o => o.GrandTotal),
                TotalVat = g.Sum(o => o.VatAmount),
                TotalDiscount = g.Sum(o => o.DiscountAmount)
            })
            .OrderBy(r => r.Period)
            .ToList();
    }

    public async Task<List<ProductSalesRow>> GetProductSalesReportAsync(ReportFilter filter)
    {
        var q = _db.OfferItems
            .Include(i => i.Product)
            .Include(i => i.Offer)
            .Where(i => i.IsSelected && i.ProductId != null
                && i.Offer!.Status != OfferStatus.Cancelled && i.Offer.Status != OfferStatus.Draft);
        if (filter.FromDate.HasValue) q = q.Where(i => i.Offer!.OfferDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) q = q.Where(i => i.Offer!.OfferDate <= filter.ToDate.Value.Date);

        var data = await q.ToListAsync();
        return data
            .Where(i => i.Product != null)
            .GroupBy(i => new { i.ProductId, i.Product!.Name, i.Product.Category })
            .Select(g => new ProductSalesRow
            {
                ProductId = g.Key.ProductId!.Value,
                ProductName = g.Key.Name,
                Category = g.Key.Category,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalAmount = g.Sum(i => i.TotalPrice)
            })
            .Where(r => string.IsNullOrWhiteSpace(filter.Category) || r.Category == filter.Category)
            .OrderByDescending(r => r.TotalAmount)
            .ToList();
    }

    public async Task<List<CustomerReportRow>> GetCustomerReportAsync(ReportFilter filter)
    {
        var q = _db.Offers.Include(o => o.Customer).Where(o => o.Status != OfferStatus.Cancelled);
        if (filter.FromDate.HasValue) q = q.Where(o => o.OfferDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) q = q.Where(o => o.OfferDate <= filter.ToDate.Value.Date);

        var data = await q.ToListAsync();
        return data
            .GroupBy(o => new { o.CustomerId, Name = o.Customer != null ? o.Customer.FullName : "-" })
            .Select(g => new CustomerReportRow
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.Name,
                OfferCount = g.Count(),
                OrderCount = g.Count(o => o.IsOrder),
                TotalAmount = g.Sum(o => o.GrandTotal)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();
    }
}
