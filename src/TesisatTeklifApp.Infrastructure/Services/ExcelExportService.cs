using System.Reflection;
using ClosedXML.Excel;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;

namespace TesisatTeklifApp.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly IOfferService _offers;

    public ExcelExportService(IOfferService offers) => _offers = offers;

    public async Task<(string FileName, byte[] Content)> ExportOffersAsync(OfferSearchFilter filter)
    {
        var offers = await _offers.SearchAsync(filter);
        var rows = offers.Select(o => new
        {
            FormNo = o.OfferNumber,
            SiparisNo = o.OrderNumber ?? "-",
            Musteri = o.Customer?.FullName ?? "-",
            Tarih = o.OfferDate.ToString("dd.MM.yyyy"),
            Durum = o.Status.ToString(),
            AraToplam = o.SubTotal,
            Iskonto = o.DiscountAmount,
            EkOranlar = o.VatAmount,
            GenelToplam = o.GrandTotal,
            Sorumlu = o.ResponsiblePerson ?? "-"
        });

        var content = ExportRows(rows, "Teklifler");
        return ($"Teklif_Listesi_{DateTime.Now:yyyyMMdd}.xlsx", content);
    }

    /// <summary>Verilen nesne listesini public property'lerine göre Excel'e döker.</summary>
    public byte[] ExportRows<T>(IEnumerable<T> rows, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(string.IsNullOrWhiteSpace(sheetName) ? "Sayfa1" : sheetName);

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Başlık satırı
        for (var c = 0; c < props.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = props[c].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3a6b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < props.Length; c++)
            {
                var value = props[c].GetValue(row);
                ws.Cell(r, c + 1).Value = ToCellValue(value);
            }
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportTable(IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string>> rows, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(string.IsNullOrWhiteSpace(sheetName) ? "Rapor" : sheetName);

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3a6b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Count; c++)
                ws.Cell(r, c + 1).Value = row[c];
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        bool b => b,
        DateTime d => d,
        decimal m => m,
        double d2 => d2,
        float f => f,
        int i => i,
        long l => l,
        Enum e => e.ToString(),
        _ => value.ToString() ?? string.Empty
    };
}
