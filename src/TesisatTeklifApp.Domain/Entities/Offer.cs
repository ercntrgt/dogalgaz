using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Teklif / sipariş ana kaydı. Hem teklif hem sipariş aynı entity üzerinde,
/// <see cref="Status"/> ile ayrışır. Tüm para alanları decimal.
/// Toplamlar OfferCalculationService tarafından hesaplanıp buraya yazılır.
/// </summary>
public class Offer : BaseEntity
{
    public string OfferNumber { get; set; } = string.Empty;   // TSF-2026-0001
    public string? OrderNumber { get; set; }                  // SPR-2026-0001 (siparişe dönünce)

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OfferDate { get; set; } = DateTime.Today;
    public OfferStatus Status { get; set; } = OfferStatus.Draft;

    // --- Bölüm toplamları (H: Fiyatlandırma Özeti) ---
    public decimal KombiKazanTotal { get; set; }
    public decimal GasInstallationTotal { get; set; }
    public decimal MaterialTotal { get; set; }
    public decimal RadiatorTotal { get; set; }
    public decimal InstallationTotal { get; set; }
    public decimal LaborTotal { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; } = 20m;
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }

    public decimal AdvancePayment { get; set; }
    public decimal RemainingPayment { get; set; }
    public bool IsVatIncluded { get; set; }

    public string? GeneralNotes { get; set; }

    // --- İş programı (J) ---
    public DateTime? WorkStartDate { get; set; }
    public DateTime? WorkEndDate { get; set; }
    public string? EmployerName { get; set; }
    public string? ResponsiblePerson { get; set; }

    // İmza (Android aşamasında müşteri imzası base64 olarak saklanacak)
    public string? CustomerSignature { get; set; }

    public ICollection<OfferItem> Items { get; set; } = new List<OfferItem>();
    public ICollection<RadiatorItem> RadiatorItems { get; set; } = new List<RadiatorItem>();
    public ICollection<PaymentPlan> PaymentPlans { get; set; } = new List<PaymentPlan>();

    public bool IsOrder => Status is OfferStatus.ConvertedToOrder
        or OfferStatus.WaitingSupply or OfferStatus.Completed;
}
