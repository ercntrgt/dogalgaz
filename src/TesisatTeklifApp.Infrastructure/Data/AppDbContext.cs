using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Constants;
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
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<OfferPhoto> OfferPhotos => Set<OfferPhoto>();
    public DbSet<Usta> Ustalar => Set<Usta>();
    public DbSet<UstaPayment> UstaPayments => Set<UstaPayment>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();

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
        builder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PurchaseOrderItem>().HasQueryFilter(e => !e.IsDeleted);

        builder.Entity<PurchaseOrder>()
            .HasOne(p => p.Supplier).WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.PurchaseOrder).WithMany(o => o.Items)
            .HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseOrder>().HasIndex(p => p.PurchaseNumber).IsUnique();

        builder.Entity<OfferPhoto>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<OfferPhoto>()
            .HasOne(p => p.Offer).WithMany()
            .HasForeignKey(p => p.OfferId).OnDelete(DeleteBehavior.Cascade);

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

        // --- Usta + Hakediş ---
        builder.Entity<Usta>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UstaPayment>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UstaPayment>()
            .HasOne(p => p.Usta).WithMany(u => u.Payments)
            .HasForeignKey(p => p.UstaId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Offer>()
            .HasOne(o => o.Usta).WithMany(u => u.Offers)
            .HasForeignKey(o => o.UstaId).OnDelete(DeleteBehavior.Restrict);

        // --- Servis kayıtları ---
        builder.Entity<ServiceRecord>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ServiceRecord>()
            .HasOne(s => s.Customer).WithMany()
            .HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ServiceRecord>()
            .HasOne(s => s.ServicedProduct).WithMany()
            .HasForeignKey(s => s.ServicedProductId).OnDelete(DeleteBehavior.SetNull);

        // Uniq index'ler
        builder.Entity<Offer>().HasIndex(o => o.OfferNumber).IsUnique();
        builder.Entity<Offer>().HasIndex(o => o.PublicToken).IsUnique();
        builder.Entity<ServiceRecord>().HasIndex(s => s.ServiceNumber).IsUnique();
        builder.Entity<Product>().HasIndex(p => new { p.Name, p.Category });
    }

    /// <summary>Kaydederken UpdatedDate alanını günceller ve metinleri büyük harfe çevirir.</summary>
    public override int SaveChanges()
    {
        BeforeSave();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        BeforeSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Bu overload override edilmezse bu yoldan kaydeden kod normalizasyonu atlar.
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        BeforeSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BeforeSave()
    {
        StampTimestamps();
        NormalizeStrings();
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

    /// <summary>
    /// Kullanıcı girdisi metinleri Türkçe büyük harfe çevirir (TextCasing.UpperFields listesi).
    /// E-posta, imza/kaşe base64, token, üretilen numaralar ve sabitle eşleşen alanlar hariçtir.
    /// </summary>
    private void NormalizeStrings()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            if (!TextCasing.UpperFields.TryGetValue(entry.Metadata.ClrType.Name, out var fields)) continue;

            foreach (var name in fields)
            {
                var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == name);
                if (prop?.CurrentValue is not string s || string.IsNullOrWhiteSpace(s)) continue;
                var upper = TextCasing.TrUpper(s);
                if (!string.Equals(upper, s, StringComparison.Ordinal)) prop.CurrentValue = upper;
            }
        }
    }
}
