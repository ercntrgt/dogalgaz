namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Ciro raporu özeti.</summary>
public class RevenueReportRow
{
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalDiscount { get; set; }
}

/// <summary>Ürün bazlı satış raporu satırı.</summary>
public class ProductSalesRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>Müşteri bazlı teklif/sipariş raporu satırı.</summary>
public class CustomerReportRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int OfferCount { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}
