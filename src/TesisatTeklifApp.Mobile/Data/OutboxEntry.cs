namespace TesisatTeklifApp.Mobile.Data;

/// <summary>
/// Senkron kuyruğu: offline yapılan değişiklikler. İnternet gelince sunucuya gönderilir.
/// </summary>
public class OutboxEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "Offer";   // Offer / Customer
    public int LocalId { get; set; }                     // yerel kayıt Id'si
    public string Operation { get; set; } = "Upsert";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Synced { get; set; }
    public DateTime? SyncedAt { get; set; }
}
