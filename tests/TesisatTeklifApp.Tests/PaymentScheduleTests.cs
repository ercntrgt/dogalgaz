using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class PaymentScheduleTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public PaymentScheduleTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var cust = new Customer { FirstName = "Ali", LastName = "Veli" };
        var offer = new Offer { OfferNumber = "T1", Customer = cust, Status = OfferStatus.Approved };
        var due = DateTime.Today.AddDays(1);
        offer.PaymentPlans.Add(new PaymentPlan { PaymentType = PaymentType.CreditCard, Amount = 1000, PaymentDate = due });
        offer.PaymentPlans.Add(new PaymentPlan { PaymentType = PaymentType.CreditCard, Amount = 1000, PaymentDate = due.AddMonths(1) });
        offer.PaymentPlans.Add(new PaymentPlan { PaymentType = PaymentType.Check, Amount = 500, PaymentDate = due });
        ctx.Offers.Add(offer);
        ctx.SaveChanges();
    }

    [Fact]
    public async Task CreditCard_Installments_Are_Not_In_Schedule_Or_Summary()
    {
        using var ctx = new AppDbContext(_options);
        var svc = new PaymentService(ctx);

        var rows = await svc.GetScheduleAsync(new PaymentScheduleFilter());
        Assert.Single(rows);
        Assert.Equal(PaymentType.Check, rows[0].PaymentType);

        var summary = await svc.GetSummaryAsync();
        Assert.Equal(500m, summary.TotalPending);
    }

    public void Dispose() => _conn.Dispose();
}
