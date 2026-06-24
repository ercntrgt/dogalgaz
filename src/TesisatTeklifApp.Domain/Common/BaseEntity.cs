namespace TesisatTeklifApp.Domain.Common;

/// <summary>
/// Tüm entity'ler için ortak temel sınıf. Soft-delete ve denetim alanları içerir.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    /// <summary>Soft-delete bayrağı. Kayıtlar fiziksel olarak silinmez.</summary>
    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }
}
