namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Excel ile toplu ürün/stok güncelleme sonucu.</summary>
public class BulkImportResult
{
    public int UpdatedCount { get; set; }
    public int StockChangedCount { get; set; }
    public int NotFoundCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Notes { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
}
