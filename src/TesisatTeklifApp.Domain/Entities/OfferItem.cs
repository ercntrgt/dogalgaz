using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Teklif satırı. B (Kombi/Kazan), C (Doğalgaz tesisatı), D (Malzeme/Ekipman)
/// ve F (Tesisat hizmetleri / İşçilik) bölümlerinin hepsi bu entity ile temsil edilir;
/// <see cref="SectionName"/> bölümü belirler.
/// </summary>
public class OfferItem : BaseEntity
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Bölüm: KombiKazan / GasInstallation / Material / Installation / Labor.</summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>Kalem adı (örn. "Ocak tesisatı", "Radyatör vanası").</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>D bölümü "kullanılsın mı?" checkbox'ı. False ise fiyatlandırmaya dahil edilmez.</summary>
    public bool IsSelected { get; set; } = true;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Adet";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Description { get; set; }

    // --- Stok kontrol alanları (sipariş aşamasında doldurulur) ---
    public decimal RequestedQuantity { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal MissingQuantity { get; set; }
    public StockStatus StockStatus { get; set; } = StockStatus.NotTracked;
    public bool IsStockInsufficient { get; set; }
    public bool IsStockDeducted { get; set; }
}
