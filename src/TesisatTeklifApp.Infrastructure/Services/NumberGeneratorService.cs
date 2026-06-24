using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

/// <summary>
/// Yıl bazlı sıralı teklif/sipariş numarası üretir (TSF-2026-0001 / SPR-2026-0001).
/// Eşzamanlılık için tek bir SemaphoreSlim + yıl içindeki en büyük numara sorgulanır.
/// (SQL Server'a taşımada bir Sequence/counter tablosu ile değiştirilebilir.)
/// </summary>
public class NumberGeneratorService : INumberGeneratorService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly AppDbContext _db;

    public NumberGeneratorService(AppDbContext db) => _db = db;

    public Task<string> GenerateOfferNumberAsync() => GenerateAsync("TSF", isOrder: false);
    public Task<string> GenerateOrderNumberAsync() => GenerateAsync("SPR", isOrder: true);

    private async Task<string> GenerateAsync(string prefix, bool isOrder)
    {
        var year = DateTime.Now.Year;
        var yearPrefix = $"{prefix}-{year}-";

        await Gate.WaitAsync();
        try
        {
            // İlgili alanın bu yıl içindeki en büyük sıra numarasını bul.
            var existing = isOrder
                ? await _db.Offers.IgnoreQueryFilters()
                    .Where(o => o.OrderNumber != null && o.OrderNumber.StartsWith(yearPrefix))
                    .Select(o => o.OrderNumber!).ToListAsync()
                : await _db.Offers.IgnoreQueryFilters()
                    .Where(o => o.OfferNumber.StartsWith(yearPrefix))
                    .Select(o => o.OfferNumber).ToListAsync();

            var max = existing
                .Select(n => int.TryParse(n.Substring(yearPrefix.Length), out var v) ? v : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{yearPrefix}{(max + 1):D4}";
        }
        finally
        {
            Gate.Release();
        }
    }
}
