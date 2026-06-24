using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Identity;

namespace TesisatTeklifApp.Infrastructure.Data;

/// <summary>
/// Hem domain hem ASP.NET Identity tablolarını barındıran tek DbContext.
/// Soft-delete global query filter ve decimal precision burada konfigüre edilir.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferItem> OfferItems => Set<OfferItem>();
    public DbSet<RadiatorItem> RadiatorItems => Set<RadiatorItem>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<StockSettings> StockSettings => Set<StockSettings>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tüm decimal alanlar için 18,2 hassasiyet (SQL Server'a taşımada da geçerli).
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Soft-delete: BaseEntity türevlerine global query filter.
        // Çocuk entity'lere de eşleşen filtre eklenir (EF uyarısını ve filtreden
        // düşen parent kaynaklı sürprizleri önlemek için).
        builder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Offer>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<OfferItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RadiatorItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PaymentPlan>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<StockMovement>().HasQueryFilter(e => !e.IsDeleted);

        // İlişkiler
        builder.Entity<Offer>()
            .HasOne(o => o.Customer).WithMany(c => c.Offers)
            .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OfferItem>()
            .HasOne(i => i.Offer).WithMany(o => o.Items)
            .HasForeignKey(i => i.OfferId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<OfferItem>()
            .HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RadiatorItem>()
            .HasOne(i => i.Offer).WithMany(o => o.RadiatorItems)
            .HasForeignKey(i => i.OfferId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PaymentPlan>()
            .HasOne(p => p.Offer).WithMany(o => o.PaymentPlans)
            .HasForeignKey(p => p.OfferId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StockMovement>()
            .HasOne(m => m.Product).WithMany()
            .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Uniq index'ler
        builder.Entity<Offer>().HasIndex(o => o.OfferNumber).IsUnique();
        builder.Entity<Product>().HasIndex(p => new { p.Name, p.Category });
    }

    /// <summary>Kaydederken UpdatedDate alanını otomatik günceller.</summary>
    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedDate = DateTime.Now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedDate = DateTime.Now;
        }
    }
}
