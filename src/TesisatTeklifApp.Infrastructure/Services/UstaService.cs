using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class UstaService : IUstaService
{
    private readonly AppDbContext _db;
    public UstaService(AppDbContext db) => _db = db;

    public Task<List<Usta>> SearchAsync(string? keyword)
    {
        var q = _db.Ustalar.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(u => u.Name.Contains(k)
                || (u.Phone != null && u.Phone.Contains(k))
                || (u.Specialty != null && u.Specialty.Contains(k)));
        }
        return q.OrderByDescending(u => u.IsActive).ThenBy(u => u.Name).ToListAsync();
    }

    public Task<List<Usta>> GetActiveAsync() =>
        _db.Ustalar.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();

    public Task<Usta?> GetByIdAsync(int id) =>
        _db.Ustalar.FirstOrDefaultAsync(u => u.Id == id);

    public async Task AddAsync(Usta usta)
    {
        _db.Ustalar.Add(usta);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Ekran ustanın bir KOPYASINI düzenler (aynı Id). Liste aynı DbContext'te takip edildiği için
    /// kopyayı Update ile eklemek "already being tracked" hatası verirdi; ayrıca kopyanın
    /// CreatedDate'i yanlış olurdu. Bu yüzden kayıtlı ustanın yalnızca düzenlenebilir alanları güncellenir.
    /// </summary>
    public async Task UpdateAsync(Usta usta)
    {
        var existing = await _db.Ustalar.FirstOrDefaultAsync(u => u.Id == usta.Id);
        if (existing is null) return;
        existing.Name = usta.Name;
        existing.Phone = usta.Phone;
        existing.Specialty = usta.Specialty;
        existing.IsActive = usta.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var u = await _db.Ustalar.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return;
        u.IsDeleted = true; u.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task<Usta> GetOrCreateByNameAsync(string name)
    {
        name = name.Trim();
        var existing = await _db.Ustalar.FirstOrDefaultAsync(u => u.Name == name);
        if (existing is not null) return existing;
        var created = new Usta { Name = name, IsActive = true };
        _db.Ustalar.Add(created);
        await _db.SaveChangesAsync();
        return created;
    }
}
