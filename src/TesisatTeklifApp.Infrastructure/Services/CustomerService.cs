using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db) => _db = db;

    public async Task<List<Customer>> SearchAsync(CustomerSearchFilter filter)
    {
        var q = _db.Customers.AsQueryable();

        // Veriler büyük harf saklandığı için arama terimleri de aynı dönüşümden geçer
        // (Postgres'te Contains harf duyarlıdır — küçük harfle arayan kullanıcı bulamazdı).
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = TextCasing.TrUpper(filter.Keyword.Trim())!;
            q = q.Where(c => c.FirstName.Contains(k) || c.LastName.Contains(k)
                || (c.Phone != null && c.Phone.Contains(k))
                || (c.NationalId != null && c.NationalId.Contains(k)));
        }
        if (!string.IsNullOrWhiteSpace(filter.FirstName))
        {
            var v = TextCasing.TrUpper(filter.FirstName.Trim())!;
            q = q.Where(c => c.FirstName.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(filter.LastName))
        {
            var v = TextCasing.TrUpper(filter.LastName.Trim())!;
            q = q.Where(c => c.LastName.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(filter.Phone))
            q = q.Where(c => c.Phone != null && c.Phone.Contains(filter.Phone));
        if (!string.IsNullOrWhiteSpace(filter.NationalId))
            q = q.Where(c => c.NationalId != null && c.NationalId.Contains(filter.NationalId));
        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var v = TextCasing.TrUpper(filter.City.Trim())!;
            q = q.Where(c => c.City != null && c.City.Contains(v));
        }
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            var v = TextCasing.TrUpper(filter.District.Trim())!;
            q = q.Where(c => c.District != null && c.District.Contains(v));
        }

        return await q.AsNoTracking().OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public Task<Customer?> GetByIdAsync(int id) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return;
        customer.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}
