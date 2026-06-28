using TesisatTeklifApp.Domain.Common;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>Teklif/siparişe yüklenen fotoğraf (saha, montaj vb.). DB'de saklanır.</summary>
public class OfferPhoto : BaseEntity
{
    public int OfferId { get; set; }
    public Offer? Offer { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string? UploadedBy { get; set; }
}
