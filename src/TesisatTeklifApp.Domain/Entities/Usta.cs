using TesisatTeklifApp.Domain.Common;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Usta (taşeron/işçi). İş bazlı hakediş bu ustalara yazılır; ödeme yapıldıkça bakiye düşer.
/// </summary>
public class Usta : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Specialty { get; set; }   // Tesisatçı, Elektrikçi, Kaynakçı...
    public bool IsActive { get; set; } = true;

    public ICollection<UstaPayment> Payments { get; set; } = new List<UstaPayment>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
