using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class UstaServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DateTime _created;   // SaveChanges eklemede CreatedDate'i kendisi damgalar

    public UstaServiceTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        var u = new Usta { Name = "Ahmet Usta", Phone = "111", IsActive = true };
        ctx.Ustalar.Add(u);
        ctx.SaveChanges();
        _created = u.CreatedDate;
    }

    [Fact]
    public async Task Update_With_Copy_After_List_Load_Does_Not_Throw()
    {
        // Ustalar sayfası: aynı DbContext ile önce liste yüklenir, sonra düzenlenen KOPYA kaydedilir.
        using (var ctx = new AppDbContext(_options))
        {
            var svc = new UstaService(ctx);
            var u = (await svc.SearchAsync(null)).Single();
            var copy = new Usta { Id = u.Id, Name = "Mehmet Usta", Phone = "222", Specialty = "Kombici", IsActive = false };
            await svc.UpdateAsync(copy);
        }

        using var check = new AppDbContext(_options);
        var saved = await check.Ustalar.SingleAsync();
        Assert.Equal("MEHMET USTA", saved.Name);
        Assert.Equal("222", saved.Phone);
        Assert.False(saved.IsActive);
        Assert.Equal(_created, saved.CreatedDate);   // kopyanın tarihi ile ezilmemeli
    }

    public void Dispose() => _conn.Dispose();
}
