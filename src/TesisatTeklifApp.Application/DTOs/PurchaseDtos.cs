namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Satınalma önerisi satırı (kritik/eksik stok bazlı).</summary>
public class PurchaseSuggestionRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal CriticalLevel { get; set; }
    public decimal MinimumLevel { get; set; }
    public decimal SuggestedQuantity { get; set; }   // önerilen alım miktarı
    public decimal LastPurchasePrice { get; set; }
    public string Reason { get; set; } = string.Empty; // "Kritik stok" / "Sipariş için eksik"
}

/// <summary>Satınalma durumu için Türkçe etiketler.</summary>
public static class PurchaseStatusText
{
    public static string Label(Domain.Enums.PurchaseStatus s) => s switch
    {
        Domain.Enums.PurchaseStatus.Draft => "Taslak",
        Domain.Enums.PurchaseStatus.Ordered => "Sipariş Verildi",
        Domain.Enums.PurchaseStatus.PartiallyReceived => "Kısmen Teslim",
        Domain.Enums.PurchaseStatus.Received => "Teslim Alındı",
        Domain.Enums.PurchaseStatus.Cancelled => "İptal",
        _ => s.ToString()
    };

    public static string Badge(Domain.Enums.PurchaseStatus s) => s switch
    {
        Domain.Enums.PurchaseStatus.Draft => "bg-secondary",
        Domain.Enums.PurchaseStatus.Ordered => "bg-primary",
        Domain.Enums.PurchaseStatus.PartiallyReceived => "bg-warning text-dark",
        Domain.Enums.PurchaseStatus.Received => "bg-success",
        Domain.Enums.PurchaseStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };
}
