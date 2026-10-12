using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Radyatör bölümü satırı. Hem panel radyatör hem vana bu entity ile temsil edilir
/// (<see cref="IsValve"/> ayırır). Toplam = <see cref="Quantity"/> × <see cref="UnitPrice"/>.
/// </summary>
public class RadiatorItem : BaseEntity
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }

    public int? RadiatorProductId { get; set; }
    public Product? RadiatorProduct { get; set; }

    public int? ValveProductId { get; set; }
    public Product? ValveProduct { get; set; }

    /// <summary>true ise bu satır bir vanadır (panel değil).</summary>
    public bool IsValve { get; set; }
    /// <summary>Kalem adı (panel: ürün adı, vana: vana ürün adı).</summary>
    public string? ItemName { get; set; }
    /// <summary>Adet (panel adedi veya vana adedi).</summary>
    public decimal Quantity { get; set; }
    /// <summary>Birim fiyat (panel: panel başına fiyat, vana: adet fiyatı).</summary>
    public decimal UnitPrice { get; set; }

    public string? RoomName { get; set; }
    public string? RadiatorBrand { get; set; }
    public string? RadiatorSize { get; set; }   // (eski) serbest ölçü metni
    public int? RadiatorHeight { get; set; }     // Yükseklik (mm)
    public int? RadiatorWidth { get; set; }      // En (mm) → Panel m = En/1000

    public decimal PanelLength { get; set; }     // metre (bilgi + toplam uzunluk)
    // --- (eski) alanlar, geriye uyum için tutulur, kullanılmaz ---
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

    /// <summary>
    /// PDF / teklif ekranlarında gösterilen kalem adı. Panel radyatörde ölçü (Yükseklik*En) yazılır;
    /// ölçü yoksa (ör. HAVLUPAN 5*100 — ad mm ölçüsü taşımaz) ürün adı kullanılır, aksi halde
    /// müşteri belgesinde yalnızca "HAVLUPAN *" kalırdı.
    /// </summary>
    public string GetDisplayName()
    {
        if (IsValve) return string.IsNullOrWhiteSpace(ItemName) ? "Vana" : ItemName.Trim();

        string? body;
        if (RadiatorHeight.HasValue && RadiatorWidth.HasValue)
            body = $"{RadiatorBrand} {RadiatorHeight}*{RadiatorWidth}";
        else if (!string.IsNullOrWhiteSpace(ItemName))
            body = ItemName;
        else if (!string.IsNullOrWhiteSpace(RadiatorSize))
            body = $"{RadiatorBrand} {RadiatorSize}";
        else
            body = RadiatorBrand;

        var text = string.Join(" ", $"{RoomName} {body}".Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return text.Length == 0 ? "Radyatör" : text;
    }
}
