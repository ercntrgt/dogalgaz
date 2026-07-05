using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

/// <summary>
/// İş bazlı usta hakedişi: her Offer.UstaEarning ustaya alacak yazar; UstaPayment düşer.
/// Bakiye = toplam hakediş − toplam ödeme.
/// </summary>
public class HakedisService : IHakedisService
{
    private readonly AppDbContext _db;
    public HakedisService(AppDbContext db) => _db = db;

    private static readonly string[] MonthsShort =
        { "", "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

    private static DateTime WeekStart(DateTime d)
    {
        int diff = ((int)d.DayOfWeek + 6) % 7;   // Pazartesi başlangıç
        return d.Date.AddDays(-diff);
    }

    private static string WeekLabel(DateTime d)
    {
        var s = WeekStart(d); var e = s.AddDays(6);
        return $"{s.Day} {MonthsShort[s.Month]} - {e.Day} {MonthsShort[e.Month]}";
    }

    private IQueryable<Offer> EarningOffers() =>
        _db.Offers.Where(o => o.UstaId != null && o.UstaEarning > 0 && o.Status != OfferStatus.Cancelled);

    public async Task<List<UstaBalanceRow>> GetUstaBalancesAsync()
    {
        var weekStart = WeekStart(DateTime.Today);

        var earnings = await EarningOffers()
            .Select(o => new { o.UstaId, o.UstaEarning, Date = o.WorkEndDate ?? o.OfferDate })
            .ToListAsync();
        var payments = await _db.UstaPayments.Select(p => new { p.UstaId, p.Amount }).ToListAsync();
        var ustalar = await _db.Ustalar.ToListAsync();

        var rows = ustalar.Select(u =>
        {
            var ue = earnings.Where(e => e.UstaId == u.Id).ToList();
            var earned = ue.Sum(e => e.UstaEarning);
            var paid = payments.Where(p => p.UstaId == u.Id).Sum(p => p.Amount);
            return new UstaBalanceRow
            {
                UstaId = u.Id,
                UstaName = u.Name,
                Specialty = u.Specialty,
                Phone = u.Phone,
                JobCount = ue.Count,
                TotalEarned = earned,
                TotalPaid = paid,
                Balance = earned - paid,
                ThisWeekEarned = ue.Where(e => e.Date.Date >= weekStart).Sum(e => e.UstaEarning)
            };
        })
        .Where(r => r.JobCount > 0 || r.TotalPaid > 0)
        .OrderByDescending(r => r.Balance)
        .ToList();

        return rows;
    }

    public async Task<List<HakedisWeekRow>> GetWeeklyScheduleAsync(HakedisFilter filter)
    {
        var q = EarningOffers().Include(o => o.Customer).Include(o => o.Usta).AsQueryable();
        if (filter.UstaId.HasValue) q = q.Where(o => o.UstaId == filter.UstaId);
        if (filter.FromDate.HasValue) q = q.Where(o => (o.WorkEndDate ?? o.OfferDate) >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) q = q.Where(o => (o.WorkEndDate ?? o.OfferDate) <= filter.ToDate.Value.Date);

        var data = await q.AsNoTracking().ToListAsync();

        return data.Select(o =>
        {
            var date = o.WorkEndDate ?? o.OfferDate;
            return new HakedisWeekRow
            {
                OfferId = o.Id,
                FormNumber = o.OrderNumber ?? o.OfferNumber,
                UstaId = o.UstaId!.Value,
                UstaName = o.Usta?.Name ?? "-",
                CustomerName = o.Customer?.FullName ?? "-",
                WorkDate = date,
                WeekLabel = WeekLabel(date),
                Earning = o.UstaEarning,
                StatusText = o.IsOrder ? "Sipariş" : "Teklif"
            };
        })
        .OrderByDescending(r => r.WorkDate)
        .ToList();
    }

    public async Task<HakedisSummary> GetSummaryAsync()
    {
        var balances = await GetUstaBalancesAsync();
        return new HakedisSummary
        {
            TotalEarned = balances.Sum(b => b.TotalEarned),
            TotalPaid = balances.Sum(b => b.TotalPaid),
            TotalBalance = balances.Sum(b => b.Balance),
            ThisWeekEarned = balances.Sum(b => b.ThisWeekEarned),
            ActiveUstaCount = balances.Count(b => b.Balance != 0)
        };
    }

    public async Task AddPaymentAsync(int ustaId, decimal amount, DateTime paidDate, string? note, string? user)
    {
        _db.UstaPayments.Add(new UstaPayment
        {
            UstaId = ustaId, Amount = amount, PaidDate = paidDate, Description = note, CreatedBy = user
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<UstaPayment>> GetPaymentsAsync(int ustaId) =>
        _db.UstaPayments.Where(p => p.UstaId == ustaId).OrderByDescending(p => p.PaidDate).ToListAsync();

    public async Task DeletePaymentAsync(int paymentId)
    {
        var p = await _db.UstaPayments.FirstOrDefaultAsync(x => x.Id == paymentId);
        if (p is null) return;
        _db.UstaPayments.Remove(p);
        await _db.SaveChangesAsync();
    }
}
