using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IStockControlService _stock;

    public ProductService(AppDbContext db, IStockControlService stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<List<Product>> SearchAsync(ProductSearchFilter filter)
    {
        var q = _db.Products.AsQueryable();

        if (filter.OnlyActive == true)
            q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            q = q.Where(p => p.Name.Contains(k)
                || (p.Brand != null && p.Brand.Contains(k))
                || (p.Model != null && p.Model.Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            q = q.Where(p => p.Category == filter.Category);

        if (filter.OnlyCriticalStock)
            q = q.Where(p => p.IsStockTracked && p.StockQuantity <= p.CriticalStockQuantity);

        return await q.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Product>> GetByCategoryAsync(string category) =>
        _db.Products.Where(p => p.IsActive && p.Category == category)
            .OrderBy(p => p.Name).ToListAsync();

    public Task<List<Product>> GetActiveAsync() =>
        _db.Products.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return;
        product.IsDeleted = true;
        product.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task AdjustStockAsync(int productId, decimal newQuantity, string? note)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return;

        var previous = product.StockQuantity;
        product.StockQuantity = newQuantity;
        await _db.SaveChangesAsync();

        await _stock.CreateStockMovementAsync(productId, null, MovementType.In,
            newQuantity - previous, note ?? "Manuel stok düzeltme");
    }
}
