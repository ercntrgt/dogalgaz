using TesisatTeklifApp.Domain.Common;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// İşlem geçmişi: hangi kaydın hangi aşamasını hangi kullanıcı ne zaman yaptı.
/// </summary>
public class ActivityLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;   // "Offer", "Purchase", "Payment"
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;       // örn. "Siparişe dönüştürüldü"
    public string? UserName { get; set; }                    // işlemi yapan kullanıcı
    public string? Description { get; set; }
}
