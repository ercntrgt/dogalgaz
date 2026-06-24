using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Bir teklifin genel stok değerlendirme özeti (sipariş detay ekranı üst uyarısı için).</summary>
public class OfferStockSummary
{
    public int InsufficientCount { get; set; }
    public int CriticalCount { get; set; }
    public bool AllSufficient => InsufficientCount == 0 && CriticalCount == 0;

    public List<InsufficientStockRow> InsufficientItems { get; set; } = new();

    /// <summary>Ekranın üstünde gösterilecek genel uyarı metinleri.</summary>
    public List<string> Messages { get; set; } = new();
}

/// <summary>"Eksik Ürünler" tablosu satırı.</summary>
public class InsufficientStockRow
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal MissingQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Stok düşme işleminin sonucu.</summary>
public class StockDeductionResult
{
    public bool Success { get; set; }
    public int DeductedItemCount { get; set; }
    public int InsufficientItemCount { get; set; }
    public bool MovedToWaitingSupply { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Bootstrap renk sınıfı yardımcıları (UI ve PDF ortak kullanır).</summary>
public static class StockStatusStyles
{
    public static string CssClass(StockStatus status) => status switch
    {
        StockStatus.Sufficient => "table-success",
        StockStatus.Critical => "table-warning",
        StockStatus.Insufficient => "table-danger",
        _ => "table-secondary"
    };

    public static string BadgeClass(StockStatus status) => status switch
    {
        StockStatus.Sufficient => "bg-success",
        StockStatus.Critical => "bg-warning text-dark",
        StockStatus.Insufficient => "bg-danger",
        _ => "bg-secondary"
    };

    public static string Label(StockStatus status) => status switch
    {
        StockStatus.Sufficient => "Yeterli",
        StockStatus.Critical => "Kritik seviyede",
        StockStatus.Insufficient => "Yetersiz",
        _ => "Stok takibi yok"
    };
}
