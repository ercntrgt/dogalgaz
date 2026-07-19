namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Offline tablet → sunucu senkron taşıyıcıları.</summary>
public class OfferSyncDto
{
    public string? OfferNumber { get; set; }
    public int Status { get; set; }
    public DateTime OfferDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? CustomerSignature { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal VatRate { get; set; } = 30m;
    public bool IsVatIncluded { get; set; }
    public string? GeneralNotes { get; set; }
    public decimal AdvancePayment { get; set; }

    public CustomerSyncDto Customer { get; set; } = new();
    public List<OfferItemSyncDto> Items { get; set; } = new();
    public List<RadiatorSyncDto> Radiators { get; set; } = new();
}

public class CustomerSyncDto
{
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? NationalId { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
}

public class OfferItemSyncDto
{
    public string SectionName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Adet";
    public decimal UnitPrice { get; set; }
    public string? Description { get; set; }
}

public class RadiatorSyncDto
{
    public string? RoomName { get; set; }
    public string? RadiatorBrand { get; set; }
    public int? RadiatorHeight { get; set; }
    public int? RadiatorWidth { get; set; }
    public decimal PanelLength { get; set; }
    public decimal ValveQuantity { get; set; }
    public decimal MeterPrice { get; set; }
    public decimal ValveUnitPrice { get; set; }
}

public class SyncPushResult
{
    public int Received { get; set; }
    public int Created { get; set; }
    public List<string> Errors { get; set; } = new();
}
