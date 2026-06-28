using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TesisatTeklifApp.Infrastructure.Services;

/// <summary>
/// Teklif/sipariş PDF'ini QuestPDF ile üretir. Kağıt forma benzer kurumsal tasarım,
/// stok yetersiz satırlar kırmızı, ayrı "Eksik Ürünler" bölümü.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private const string Navy = "#1a3a6b";
    private const string Gray = "#595959";
    private const string LightGray = "#f2f2f2";
    private const string Red = "#c0392b";

    private readonly AppDbContext _db;
    private readonly IStockControlService _stock;
    private readonly string? _logoPath;

    public PdfExportService(AppDbContext db, IStockControlService stock, string? logoPath = null)
    {
        _db = db;
        _stock = stock;
        _logoPath = logoPath;
    }

    public async Task<(string FileName, byte[] Content)> GenerateOfferPdfAsync(int offerId)
    {
        var offer = await _db.Offers
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new InvalidOperationException("Teklif bulunamadı.");

        var summary = await _stock.CheckOfferStockAvailabilityAsync(offer);
        var isOrder = offer.IsOrder;

        byte[] logo = _logoPath is not null && File.Exists(_logoPath)
            ? await File.ReadAllBytesAsync(_logoPath) : Array.Empty<byte>();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Helvetica"));

                page.Header().Element(h => Header(h, offer, isOrder, logo));
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(8);
                    CustomerBlock(col, offer);
                    SectionTable(col, "Kombi / Kazan", offer, OfferSections.KombiKazan);
                    SectionTable(col, "Doğalgaz Tesisatı", offer, OfferSections.GasInstallation);
                    SectionTable(col, "Malzeme / Ekipman", offer, OfferSections.Material);
                    RadiatorTable(col, offer);
                    SectionTable(col, "Tesisat Hizmetleri", offer, OfferSections.Installation);
                    SectionTable(col, "İşçilik", offer, OfferSections.Labor);
                    if (!string.IsNullOrWhiteSpace(offer.GeneralNotes))
                        NotesBlock(col, offer.GeneralNotes!);
                    PricingSummary(col, offer);
                    PaymentPlanBlock(col, offer);
                    WorkProgramBlock(col, offer);
                    if (summary.InsufficientItems.Count > 0)
                        MissingProductsBlock(col, summary.InsufficientItems);
                    SignatureBlock(col, offer.CustomerSignature);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("ÖZDEMİR Mühendislik Mekanik Tesisat  •  ").FontColor(Gray);
                    t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
                });
            });
        });

        var bytes = doc.GeneratePdf();
        var safeName = (offer.Customer?.FullName ?? "Musteri").Replace(" ", "_");
        var prefix = isOrder ? "Siparis_Formu" : "Teklif_Formu";
        var fileName = $"{prefix}_{safeName}_{offer.OfferDate:yyyyMMdd}.pdf";
        return (fileName, bytes);
    }

    private void Header(IContainer h, Offer offer, bool isOrder, byte[] logo)
    {
        h.Row(row =>
        {
            if (logo.Length > 0)
                row.ConstantItem(150).AlignMiddle().Image(logo).FitWidth();
            else
                row.ConstantItem(150).AlignMiddle().Text("ÖZDEMİR").FontSize(20).Bold().FontColor(Navy);

            row.RelativeItem().AlignRight().Column(c =>
            {
                c.Item().Text(isOrder ? "SİPARİŞ FORMU" : "TEKLİF FORMU")
                    .FontSize(16).Bold().FontColor(Navy);
                c.Item().Text("Doğalgaz / Kombi / Radyatör").FontSize(9).FontColor(Gray);
                c.Item().PaddingTop(4).Text($"Form No: {offer.OfferNumber}").Bold();
                if (isOrder && offer.OrderNumber is not null)
                    c.Item().Text($"Sipariş No: {offer.OrderNumber}").Bold();
                c.Item().Text($"Tarih: {offer.OfferDate:dd.MM.yyyy}");
            });
        });
    }

    private void CustomerBlock(ColumnDescriptor col, Offer offer)
    {
        var cust = offer.Customer;
        Card(col, "Müşteri Bilgileri", inner =>
        {
            inner.Item().Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Ad Soyad: {cust?.FullName}");
                    c.Item().Text($"T.C. No: {cust?.NationalId ?? "-"}");
                    c.Item().Text($"Telefon: {cust?.Phone ?? "-"}");
                });
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text($"İl / İlçe: {cust?.City} / {cust?.District}");
                    c.Item().Text($"Adres: {cust?.Address ?? "-"}");
                    c.Item().Text($"Sorumlu: {offer.ResponsiblePerson ?? "-"}");
                });
            });
        });
    }

    private void SectionTable(ColumnDescriptor col, string title, Offer offer, string section)
    {
        // 0 adet/metre olan kalemler PDF'e girmez.
        var items = offer.Items
            .Where(i => i.SectionName == section && i.IsSelected && (i.Quantity > 0 || i.TotalPrice > 0))
            .ToList();
        if (items.Count == 0) return;

        Card(col, title, inner =>
        {
            inner.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(20); c.RelativeColumn(3); c.ConstantColumn(45);
                    c.ConstantColumn(40); c.ConstantColumn(60); c.ConstantColumn(65); c.ConstantColumn(70);
                });
                TableHeader(table, "#", "Kalem", "Adet", "Birim", "B.Fiyat", "Toplam", "Stok");

                var i = 1;
                foreach (var it in items)
                {
                    var bg = it.IsStockInsufficient ? Red : (i % 2 == 0 ? LightGray : "#ffffff");
                    var fc = it.IsStockInsufficient ? "#ffffff" : "#000000";
                    Cell(table, bg, fc, (i++).ToString());
                    Cell(table, bg, fc, it.ItemName);
                    Cell(table, bg, fc, Num(it.Quantity));
                    Cell(table, bg, fc, it.Unit);
                    Cell(table, bg, fc, Money(it.UnitPrice));
                    Cell(table, bg, fc, Money(it.TotalPrice));
                    Cell(table, bg, fc, it.IsStockInsufficient ? $"Eksik {Num(it.MissingQuantity)}" : "-");
                }
            });
        });
    }

    private void RadiatorTable(ColumnDescriptor col, Offer offer)
    {
        // Boş (0 panel ve 0 vana) radyatör satırları PDF'e girmez.
        var radItems = offer.RadiatorItems
            .Where(r => r.PanelLength > 0 || r.ValveQuantity > 0 || r.TotalPrice > 0)
            .ToList();
        if (radItems.Count == 0) return;
        Card(col, "Radyatör", inner =>
        {
            inner.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); c.RelativeColumn(2); c.ConstantColumn(55);
                    c.ConstantColumn(45); c.ConstantColumn(60); c.ConstantColumn(70);
                });
                TableHeader(table, "Oda", "Marka/Ölçü", "Panel(m)", "Vana", "Metre F.", "Toplam");
                var i = 0;
                foreach (var r in radItems)
                {
                    var bg = r.IsStockInsufficient ? Red : (i++ % 2 == 0 ? LightGray : "#ffffff");
                    var fc = r.IsStockInsufficient ? "#ffffff" : "#000000";
                    Cell(table, bg, fc, r.RoomName ?? "-");
                    var olcu = (r.RadiatorHeight.HasValue || r.RadiatorWidth.HasValue)
                        ? $"{r.RadiatorHeight}*{r.RadiatorWidth}" : r.RadiatorSize;
                    Cell(table, bg, fc, $"{r.RadiatorBrand} {olcu}");
                    Cell(table, bg, fc, Num(r.PanelLength));
                    Cell(table, bg, fc, Num(r.ValveQuantity));
                    Cell(table, bg, fc, Money(r.MeterPrice));
                    Cell(table, bg, fc, Money(r.TotalPrice));
                }
            });
            var totalPanel = radItems.Sum(r => r.PanelLength);
            inner.Item().PaddingTop(3).AlignRight().Text($"Toplam panel uzunluğu: {Num(totalPanel)} m").Italic();
        });
    }

    private void PricingSummary(ColumnDescriptor col, Offer offer)
    {
        Card(col, "Fiyatlandırma Özeti", inner =>
        {
            inner.Item().Row(r =>
            {
                r.RelativeItem();
                r.ConstantItem(240).Column(c =>
                {
                    Line(c, "Kombi / Kazan", offer.KombiKazanTotal);
                    Line(c, "Doğalgaz Tesisatı", offer.GasInstallationTotal);
                    Line(c, "Malzeme / Ekipman", offer.MaterialTotal);
                    Line(c, "Radyatör", offer.RadiatorTotal);
                    Line(c, "Tesisat Hizmetleri", offer.InstallationTotal);
                    Line(c, "İşçilik", offer.LaborTotal);
                    c.Item().PaddingVertical(2).LineHorizontal(0.5f);
                    Line(c, "Ara Toplam", offer.SubTotal);
                    Line(c, $"İskonto (%{offer.DiscountRate})", offer.DiscountAmount);
                    Line(c, $"KDV (%{offer.VatRate}){(offer.IsVatIncluded ? " - dahil" : "")}", offer.VatAmount);
                    c.Item().PaddingVertical(2).LineHorizontal(0.5f);
                    c.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("GENEL TOPLAM").Bold().FontColor(Navy);
                        rr.ConstantItem(90).AlignRight().Text(Money(offer.GrandTotal)).Bold().FontColor(Navy);
                    });
                    Line(c, "Peşinat", offer.AdvancePayment);
                    Line(c, "Kalan Ödeme", offer.RemainingPayment);
                });
            });
        });
    }

    private void PaymentPlanBlock(ColumnDescriptor col, Offer offer)
    {
        if (offer.PaymentPlans.Count == 0) return;
        Card(col, "Ödeme Planı", inner =>
        {
            foreach (var p in offer.PaymentPlans)
                inner.Item().Text($"{PaymentLabel(p.PaymentType)}: {Money(p.Amount)}" +
                    (p.PaymentDate.HasValue ? $" - {p.PaymentDate:dd.MM.yyyy}" : "") +
                    (string.IsNullOrWhiteSpace(p.Description) ? "" : $" ({p.Description})"));
        });
    }

    private void WorkProgramBlock(ColumnDescriptor col, Offer offer)
    {
        Card(col, "İş Programı", inner =>
        {
            inner.Item().Row(r =>
            {
                r.RelativeItem().Text($"Başlangıç: {offer.WorkStartDate:dd.MM.yyyy}");
                r.RelativeItem().Text($"Teslim: {offer.WorkEndDate:dd.MM.yyyy}");
                r.RelativeItem().Text($"İşveren: {offer.EmployerName ?? "-"}");
            });
        });
    }

    private void MissingProductsBlock(ColumnDescriptor col, List<Application.DTOs.InsufficientStockRow> rows)
    {
        col.Item().Background(LightGray).Border(1).BorderColor(Red).Padding(6).Column(inner =>
        {
            inner.Item().Text("Eksik Ürünler").Bold().FontColor(Red).FontSize(11);
            inner.Item().PaddingBottom(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3); c.RelativeColumn(2); c.ConstantColumn(55);
                    c.ConstantColumn(55); c.ConstantColumn(55); c.ConstantColumn(45);
                });
                TableHeader(table, "Ürün", "Kategori", "Talep", "Mevcut", "Eksik", "Birim");
                foreach (var r in rows)
                {
                    Cell(table, "#ffffff", "#000000", r.ProductName);
                    Cell(table, "#ffffff", "#000000", r.Category);
                    Cell(table, "#ffffff", "#000000", Num(r.RequestedQuantity));
                    Cell(table, "#ffffff", "#000000", Num(r.AvailableStock));
                    Cell(table, "#ffffff", Red, Num(r.MissingQuantity));
                    Cell(table, "#ffffff", "#000000", r.Unit);
                }
            });
            inner.Item().Text(
                "Stok yetersiz olarak işaretlenen ürünler tedarik sürecine alınmalıdır. " +
                "Bu ürünler stok tamamlanmadan uygulama / montaj planına dahil edilmemelidir.")
                .Italic().FontColor(Red).FontSize(8);
        });
    }

    private void NotesBlock(ColumnDescriptor col, string notes) =>
        Card(col, "Genel Açıklamalar", inner => inner.Item().Text(notes));

    private void SignatureBlock(ColumnDescriptor col, string? customerSignature)
    {
        var sigBytes = DecodeSignature(customerSignature);
        col.Item().PaddingTop(20).Row(r =>
        {
            r.RelativeItem().Column(c =>
            {
                if (sigBytes is not null)
                    c.Item().Height(50).AlignCenter().Image(sigBytes).FitHeight();
                else
                    c.Item().Height(50);
                c.Item().LineHorizontal(0.7f);
                c.Item().AlignCenter().Text("Müşteri İmza").FontColor(Gray);
            });
            r.ConstantItem(40);
            r.RelativeItem().Column(c =>
            {
                c.Item().Height(50);
                c.Item().LineHorizontal(0.7f);
                c.Item().AlignCenter().Text("Firma Yetkilisi İmza").FontColor(Gray);
            });
        });
    }

    /// <summary>"data:image/png;base64,..." biçimindeki imzayı bayt dizisine çevirir.</summary>
    private static byte[]? DecodeSignature(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        try
        {
            var idx = dataUrl.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            var b64 = idx >= 0 ? dataUrl[(idx + 7)..] : dataUrl;
            return Convert.FromBase64String(b64);
        }
        catch { return null; }
    }

    // ---------------- yardımcılar ----------------
    private void Card(ColumnDescriptor col, string title, Action<ColumnDescriptor> body)
    {
        col.Item().Column(c =>
        {
            c.Item().Background(Navy).Padding(4).Text(title).FontColor("#ffffff").Bold();
            c.Item().Border(0.5f).BorderColor(Gray).Padding(6).Column(body);
        });
    }

    private static void TableHeader(TableDescriptor table, params string[] headers)
    {
        table.Header(h =>
        {
            foreach (var head in headers)
                h.Cell().Background(Navy).Padding(3).Text(head).FontColor("#ffffff").Bold().FontSize(8);
        });
    }

    private static void Cell(TableDescriptor table, string bg, string fc, string text) =>
        table.Cell().Background(bg).Padding(3).Text(text).FontColor(fc).FontSize(8);

    private static void Line(ColumnDescriptor c, string label, decimal value) =>
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label);
            r.ConstantItem(90).AlignRight().Text(Money(value));
        });

    private static string Money(decimal v) => v.ToString("#,##0.00") + " ₺";
    private static string Num(decimal v) => v % 1 == 0 ? v.ToString("0") : v.ToString("0.##");

    private static string PaymentLabel(PaymentType t) => t switch
    {
        PaymentType.Cash => "Nakit",
        PaymentType.CreditCard => "Kredi Kartı",
        _ => "Diğer"
    };
}
