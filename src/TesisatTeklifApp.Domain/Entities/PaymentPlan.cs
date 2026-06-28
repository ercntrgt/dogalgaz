using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>Ödeme planı satırı (I). Bir teklifin birden fazla ödeme satırı olabilir.</summary>
public class PaymentPlan : BaseEntity
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }

    public PaymentType PaymentType { get; set; } = PaymentType.Cash;
    public decimal Amount { get; set; }

    /// <summary>Ödemenin planlanan/vade tarihi.</summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>Tahsil edildi mi?</summary>
    public bool IsPaid { get; set; }

    /// <summary>Tahsil edildiği gerçek tarih.</summary>
    public DateTime? PaidDate { get; set; }

    public string? Description { get; set; }
}
