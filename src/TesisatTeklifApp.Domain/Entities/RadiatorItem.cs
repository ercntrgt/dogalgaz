using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Radyatör bölümü (E) satırı. Panel + vana toplamı ayrı hesaplanır.
/// </summary>
public class RadiatorItem : BaseEntity
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }

    public int? RadiatorProductId { get; set; }
    public Product? RadiatorProduct { get; set; }

    public int? ValveProductId { get; set; }
    public Product? ValveProduct { get; set; }

    public string? RoomName { get; set; }
    public string? RadiatorBrand { get; set; }
    public string? RadiatorSize { get; set; }   // (eski) serbest ölçü metni
    public int? RadiatorHeight { get; set; }     // Yükseklik (mm)
    public int? RadiatorWidth { get; set; }      // En (mm)

    public decimal PanelLength { get; set; }
    public decimal ValveQuantity { get; set; }
    public decimal MeterPrice { get; set; }
    public decimal ValveUnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public string? Description { get; set; }

    // --- Stok kontrol alanları ---
    public decimal AvailableStock { get; set; }
    public decimal MissingQuantity { get; set; }
    public StockStatus StockStatus { get; set; } = StockStatus.NotTracked;
    public bool IsStockInsufficient { get; set; }
    public bool IsStockDeducted { get; set; }
}
