using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Ürün arama filtresi.</summary>
public class ProductSearchFilter
{
    public string? Keyword { get; set; }     // ad / marka / model
    public string? Category { get; set; }
    public bool? OnlyActive { get; set; } = true;
    public bool OnlyCriticalStock { get; set; }
}

/// <summary>Müşteri arama filtresi.</summary>
public class CustomerSearchFilter
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? NationalId { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Keyword { get; set; }
}

/// <summary>Teklif / sipariş listesi filtresi.</summary>
public class OfferSearchFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CustomerName { get; set; }
    public string? FormNumber { get; set; }
    public OfferStatus? Status { get; set; }
    public string? ResponsiblePerson { get; set; }
    public bool OnlyOrders { get; set; }

    /// <summary>"Teklifler" listesi için: siparişe dönüşmüş kayıtları gizler.</summary>
    public bool ExcludeOrders { get; set; }

    /// <summary>Yalnızca bu kullanıcının oluşturduğu kayıtlar (satış personeli kısıtı).</summary>
    public string? CreatedBy { get; set; }
}

/// <summary>Rapor filtresi (eksik/kritik stok, ciro vb.).</summary>
public class ReportFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Category { get; set; }
    public OfferStatus? Status { get; set; }
    public bool OnlyInsufficientStock { get; set; }
    public bool OnlyCriticalStock { get; set; }
}
