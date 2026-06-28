using System.Text.Json;
using Microsoft.Maui.Storage;
using TesisatTeklifApp.Domain.Entities;

namespace TesisatTeklifApp.Mobile.Data;

/// <summary>
/// İlk açılışta paketteki tam katalog (catalog.json) yerel veritabanına yüklenir; böylece
/// saha internetsiz tüm ürünleri görür. Senkron geldiğinde sunucu kataloğu güncelleyecek.
/// </summary>
public static class LocalSeed
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static async Task RunAsync(LocalDbContext db)
    {
        if (db.Products.Any()) return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("catalog.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var items = JsonSerializer.Deserialize<List<CatalogItem>>(json, Opts) ?? new();

            foreach (var c in items)
            {
                db.Products.Add(new Product
                {
                    Name = c.Name ?? "",
                    Category = string.IsNullOrWhiteSpace(c.Category) ? "Diğer" : c.Category,
                    Brand = c.Brand,
                    Model = c.Model,
                    Unit = string.IsNullOrWhiteSpace(c.Unit) ? "Adet" : c.Unit,
                    PurchasePrice = (decimal)c.PurchasePrice,
                    SalePrice = (decimal)c.SalePrice,
                    VatRate = c.VatRate == 0 ? 20m : (decimal)c.VatRate,
                    IsActive = true,
                    IsStockTracked = false
                });
            }
            await db.SaveChangesAsync();
        }
        catch
        {
            // Katalog yüklenemezse en azından birkaç örnek ürün ekle.
            db.Products.Add(new Product { Name = "Kombi (örnek)", Category = "Kombi", Unit = "Adet", SalePrice = 30000, VatRate = 20, IsActive = true });
            db.Products.Add(new Product { Name = "İşçilik (örnek)", Category = "İşçilik", Unit = "Adet", SalePrice = 5000, VatRate = 20, IsActive = true });
            await db.SaveChangesAsync();
        }
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
}
