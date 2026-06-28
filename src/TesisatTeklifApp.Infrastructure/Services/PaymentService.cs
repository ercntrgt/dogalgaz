using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db) => _db = db;

    public async Task<List<PaymentScheduleRow>> GetScheduleAsync(PaymentScheduleFilter filter)
    {
        var today = DateTime.Today;

        var q = _db.PaymentPlans
            .Include(p => p.Offer).ThenInclude(o => o!.Customer)
            .Where(p => p.Offer != null && p.Offer.Status != OfferStatus.Cancelled);

        if (filter.FromDate.HasValue)
            q = q.Where(p => p.PaymentDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            q = q.Where(p => p.PaymentDate <= filter.ToDate.Value.Date);
        if (filter.OnlyUnpaid)
            q = q.Where(p => !p.IsPaid);

        var data = await q.AsNoTracking().ToListAsync();

        var rows = data.Select(p =>
        {
            var row = new PaymentScheduleRow
            {
                PaymentPlanId = p.Id,
                OfferId = p.OfferId,
                FormNumber = p.Offer!.OrderNumber ?? p.Offer.OfferNumber,
                CustomerId = p.Offer.CustomerId,
                CustomerName = p.Offer.Customer?.FullName ?? "-",
                PaymentType = p.PaymentType,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                IsPaid = p.IsPaid,
                PaidDate = p.PaidDate,
                Description = p.Description
            };
            row.Status = Status(p.IsPaid, p.PaymentDate, today);
            row.DaysRemaining = p.PaymentDate.HasValue
                ? (int)(p.PaymentDate.Value.Date - today).TotalDays
                : null;
            return row;
        });

        if (filter.OnlyOverdue)
            rows = rows.Where(r => r.Status == PaymentScheduleStatus.Overdue);

        return rows
            .OrderBy(r => r.PaymentDate ?? DateTime.MaxValue)
            .ToList();
    }

    public async Task<PaymentScheduleSummary> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthEnd = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);

        var data = await _db.PaymentPlans
            .Include(p => p.Offer)
            .Where(p => p.Offer != null && p.Offer.Status != OfferStatus.Cancelled)
            .AsNoTracking()
            .ToListAsync();

        return new PaymentScheduleSummary
        {
            PaidAmount = data.Where(p => p.IsPaid).Sum(p => p.Amount),
            TotalPending = data.Where(p => !p.IsPaid).Sum(p => p.Amount),
            OverdueAmount = data.Where(p => !p.IsPaid && p.PaymentDate.HasValue && p.PaymentDate.Value.Date < today).Sum(p => p.Amount),
            ThisMonthAmount = data.Where(p => !p.IsPaid && p.PaymentDate.HasValue
                && p.PaymentDate.Value.Date >= today && p.PaymentDate.Value.Date <= monthEnd).Sum(p => p.Amount)
        };
    }

    public async Task MarkPaidAsync(int paymentPlanId, bool isPaid)
    {
        var plan = await _db.PaymentPlans.FirstOrDefaultAsync(p => p.Id == paymentPlanId);
        if (plan is null) return;
        plan.IsPaid = isPaid;
        plan.PaidDate = isPaid ? DateTime.Today : null;
        await _db.SaveChangesAsync();
    }

    private static PaymentScheduleStatus Status(bool isPaid, DateTime? date, DateTime today)
    {
        if (isPaid) return PaymentScheduleStatus.Paid;
        if (!date.HasValue) return PaymentScheduleStatus.NoDate;
        if (date.Value.Date < today) return PaymentScheduleStatus.Overdue;
        if (date.Value.Date == today) return PaymentScheduleStatus.DueToday;
        return PaymentScheduleStatus.Upcoming;
    }
}
