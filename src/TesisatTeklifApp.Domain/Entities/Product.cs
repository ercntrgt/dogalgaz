using TesisatTeklifApp.Domain.Common;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Ürün / malzeme / hizmet tanımı. Fiyat ve stok bilgilerini taşır.
/// Stok miktarları metre ile takip edilebildiği için decimal tutulur.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string Unit { get; set; } = "Adet";

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal VatRate { get; set; } = 20m;

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // --- Stok alanları ---
    public bool IsStockTracked { get; set; } = true;
    public decimal StockQuantity { get; set; }
    public decimal MinimumStockQuantity { get; set; }
    public decimal CriticalStockQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }

    /// <summary>Mevcut - rezerve = gerçekte tahsis edilebilir miktar.</summary>
    public decimal AvailableQuantity => StockQuantity - ReservedQuantity;
}
