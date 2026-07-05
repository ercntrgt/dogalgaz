using TesisatTeklifApp.Domain.Common;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Ustaya yapılan ödeme. Toplam hakediş − toplam ödeme = usta bakiyesi.
/// </summary>
public class UstaPayment : BaseEntity
{
    public int UstaId { get; set; }
    public Usta? Usta { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; } = DateTime.Today;
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
}
