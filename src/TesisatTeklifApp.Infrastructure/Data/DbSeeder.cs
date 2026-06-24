using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await db.Database.MigrateAsync();

        // --- Roller ---
        foreach (var role in AppRoles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // --- Demo kullanıcılar (her rol için bir hesap) ---
        await EnsureUser(userManager, "admin@ozdemir.local", "Admin!123", "Sistem Yöneticisi", AppRoles.Admin);
        await EnsureUser(userManager, "satis@ozdemir.local", "Satis!123", "Satış Personeli", AppRoles.SalesPerson);
        await EnsureUser(userManager, "gozlemci@ozdemir.local", "Gozlem!123", "Görüntüleyici", AppRoles.Viewer);

        // --- Stok ayarı (tek satır) ---
        if (!await db.StockSettings.AnyAsync())
        {
            db.StockSettings.Add(new StockSettings());
            await db.SaveChangesAsync();
        }

        // --- Örnek ürünler ---
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(SampleProducts());
            await db.SaveChangesAsync();
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
