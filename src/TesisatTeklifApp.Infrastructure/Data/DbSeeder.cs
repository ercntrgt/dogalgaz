using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Identity;

namespace TesisatTeklifApp.Infrastructure.Data;

/// <summary>
/// İlk açılışta rolleri, demo kullanıcıları, stok ayarını ve örnek ürünleri ekler.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db,
        UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        string? contentRootPath = null)
    {
        // SQLite: migration geçmişiyle.
        // Postgres/SQL Server: Neon vb. veritabanını önceden oluşturur; EnsureCreated bu durumda
        // tabloları KURMAZ. Bu yüzden DB'yi (yoksa) oluştur + tablo yoksa modelden tabloları kur.
        if (db.Database.IsSqlite())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            var creator = db.GetService<IRelationalDatabaseCreator>();
            if (!await creator.ExistsAsync())
                await creator.CreateAsync();

            // Uygulama şeması kurulu mu? Anahtar tablo (AspNetRoles) yoksa şema eksik/kısmi
            // (Neon DB'si zaten var + önceki denemelerden kalan parçalar olabilir).
            // Bu durumda public şemayı temizleyip modelden tüm tabloları sıfırdan kur.
            // (Bu aşamada henüz gerçek veri yoktur; AspNetRoles varsa bu blok atlanır.)
            if (!await TableExistsAsync(db, "AspNetRoles"))
            {
                await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
                await creator.CreateTablesAsync();
            }
        }

        // --- Roller ---
        foreach (var role in AppRoles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // --- Kullanıcılar. Yönetici + Satış. (Görüntüleyici hesabı yok; müşteri onayı özel link ile.) ---
        await EnsureUser(userManager, "admin@ozdemir.local", "Admin!123", "Sistem Yöneticisi", AppRoles.Admin);
        await EnsureUser(userManager, "satis@ozdemir.local", "Satis!123", "Satış Personeli", AppRoles.SalesPerson);

        // Görüntüleyici hesabı artık kullanılmıyor — mevcut kurulumlarda varsa temizle.
        foreach (var v in await userManager.GetUsersInRoleAsync(AppRoles.Viewer))
            await userManager.DeleteAsync(v);

        // --- Stok ayarı (tek satır) ---
        if (!await db.StockSettings.AnyAsync())
        {
            db.StockSettings.Add(new StockSettings());
            await db.SaveChangesAsync();
        }

        // --- Örnek ustalar ---
        if (!await db.Ustalar.AnyAsync())
        {
            db.Ustalar.AddRange(
                new Usta { Name = "Ahmet Usta", Specialty = "Tesisatçı", IsActive = true },
                new Usta { Name = "Mehmet Usta", Specialty = "Kombici", IsActive = true });
            await db.SaveChangesAsync();
        }

        // --- Ürünler: mevcut katalog (165) yoksa dosyadan yükle, o da yoksa örnekler ---
        if (await db.Products.CountAsync() < 15)
        {
            var seeded = LoadCatalog(contentRootPath);
            db.Products.AddRange(seeded.Count > 0 ? seeded : SampleProducts());
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Web/Data/catalog.json (165 ürün) — yeni/temiz veritabanları için.</summary>
    private static List<Product> LoadCatalog(string? contentRootPath)
    {
        try
        {
            var path = Path.Combine(contentRootPath ?? ".", "Data", "catalog.json");
            if (!File.Exists(path)) return new();
            var json = File.ReadAllText(path);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<CatalogItem>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            return items.Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => new Product
            {
                Name = c.Name!, Category = string.IsNullOrWhiteSpace(c.Category) ? ProductCategories.Diger : c.Category!,
                Brand = c.Brand, Model = c.Model, Unit = string.IsNullOrWhiteSpace(c.Unit) ? "Adet" : c.Unit!,
                PurchasePrice = (decimal)c.PurchasePrice, SalePrice = (decimal)c.SalePrice,
                VatRate = c.VatRate == 0 ? 20m : (decimal)c.VatRate,
                IsActive = true, IsStockTracked = false
            }).ToList();
        }
        catch { return new(); }
    }

    private class CatalogItem
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Unit { get; set; }
        public double PurchasePrice { get; set; }
        public double SalePrice { get; set; }
        public double VatRate { get; set; }
    }

    /// <summary>PostgreSQL'de belirtilen tablo public şemada var mı? (to_regclass ile).</summary>
    private static async Task<bool> TableExistsAsync(AppDbContext db, string table)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT to_regclass('public.\"{table}\"') IS NOT NULL";
            var result = await cmd.ExecuteScalarAsync();
            return result is bool b && b;
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> userManager,
        string email, string password, string fullName, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FullName = fullName, IsActive = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static List<Product> SampleProducts() => new()
    {
        P("Vaillant EcoTec Kombi", ProductCategories.Kombi, "Vaillant", "EcoTec", "Adet", 32000m, 5, 2),
        P("Demirdöküm Nitromix Kombi", ProductCategories.Kombi, "Demirdöküm", "Nitromix", "Adet", 28000m, 8, 3),
        P("Demirdöküm Panel Radyatör", ProductCategories.Radyator, "Demirdöküm", "Panel", "Metre", 2500m, 120, 30),
        P("ECA Termostatik Vana", ProductCategories.TermostatikVana, "ECA", null, "Adet", 450m, 60, 15),
        P("Kombi Dolabı Standart", ProductCategories.KombiDolabi, "Standart", null, "Adet", 1800m, 20, 5),
        P("Kablosuz Oda Termostatı", ProductCategories.OdaTermostati, "Kablosuz", null, "Adet", 2500m, 15, 5),
        P("Gaz Alarm Cihazı", ProductCategories.GazAlarmCihazi, "Standart", null, "Adet", 900m, 25, 8),
        P("Cam Menfezi", ProductCategories.CamMenfezi, "Standart", null, "Adet", 350m, 40, 10),
        // Hizmet kalemleri - stok takipsiz
        S("Standart Montaj İşçiliği", ProductCategories.Iscilik, 5000m),
        S("Full Tesisat", ProductCategories.FullTesisat, 15000m),
        S("Hat Taşıma", ProductCategories.HatTasima, 3500m),
        S("Ek Hat Alma", ProductCategories.EkHatAlma, 2500m),
    };

    private static Product P(string name, string category, string? brand, string? model,
        string unit, decimal sale, decimal stock, decimal critical) => new()
    {
        Name = name, Category = category, Brand = brand, Model = model, Unit = unit,
        PurchasePrice = Math.Round(sale * 0.8m, 2), SalePrice = sale, VatRate = 20m,
        IsActive = true, IsStockTracked = true,
        StockQuantity = stock, MinimumStockQuantity = critical, CriticalStockQuantity = critical
    };

    private static Product S(string name, string category, decimal sale) => new()
    {
        Name = name, Category = category, Unit = "Adet",
        PurchasePrice = 0, SalePrice = sale, VatRate = 20m,
        IsActive = true, IsStockTracked = false
    };
}
