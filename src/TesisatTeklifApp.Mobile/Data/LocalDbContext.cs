using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Entities;

namespace TesisatTeklifApp.Mobile.Data;

/// <summary>
/// Tablet üzerinde çevrimdışı (offline) yerel veritabanı. ASP.NET Identity'ye bağlı
/// DEĞİLDİR — yalnızca domain verisi. İnternet gelince senkron servisi sunucuya iletir.
/// </summary>
public class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferItem> OfferItems => Set<OfferItem>();
    public DbSet<RadiatorItem> RadiatorItems => Set<RadiatorItem>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<OutboxEntry> Outbox => Set<OutboxEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var p in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            p.SetPrecision(18);
            p.SetScale(2);
        }

        // İlişkiler (Identity yok)
        builder.Entity<Offer>().HasMany(o => o.Items).WithOne(i => i.Offer!).HasForeignKey(i => i.OfferId);
        builder.Entity<Offer>().HasMany(o => o.RadiatorItems).WithOne(r => r.Offer!).HasForeignKey(r => r.OfferId);
        builder.Entity<Offer>().HasMany(o => o.PaymentPlans).WithOne(p => p.Offer!).HasForeignKey(p => p.OfferId);
        builder.Entity<Offer>().HasOne(o => o.Customer).WithMany(c => c.Offers).HasForeignKey(o => o.CustomerId);

        builder.Entity<OfferItem>().HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.SetNull);
    }
}
