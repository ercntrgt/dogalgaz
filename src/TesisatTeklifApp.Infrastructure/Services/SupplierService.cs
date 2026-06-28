using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    public SupplierService(AppDbContext db) => _db = db;

    public Task<List<Supplier>> SearchAsync(string? keyword)
    {
        var q = _db.Suppliers.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(s => s.Name.Contains(k)
                || (s.Phone != null && s.Phone.Contains(k))
                || (s.TaxNumber != null && s.TaxNumber.Contains(k)));
        }
        return q.OrderBy(s => s.Name).ToListAsync();
    }

    public Task<Supplier?> GetByIdAsync(int id) =>
        _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(Supplier supplier)
    {
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _db.Suppliers.Update(supplier);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var s = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return;
        s.IsDeleted = true; s.IsActive = false;
        await _db.SaveChangesAsync();
    }
}
