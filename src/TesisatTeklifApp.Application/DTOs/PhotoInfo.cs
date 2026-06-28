namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Fotoğraf meta verisi (byte içermez; galeri listesi için).</summary>
public class PhotoInfo
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? UploadedBy { get; set; }
}
