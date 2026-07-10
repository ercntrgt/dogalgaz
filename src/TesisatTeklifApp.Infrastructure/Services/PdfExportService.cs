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

    public async Task<(string FileName, byte[] Content)> GenerateOfferPdfAsync(int offerId, bool includeLinePrices = true)
    {
        var offer = await _db.Offers
            .Include(o => o.Customer)
            .Include(o => o.Usta)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.RadiatorItems)
            .Include(o => o.PaymentPlans)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new InvalidOperationException("Teklif bulunamadı.");

        var summary = await _stock.CheckOfferStockAvailabilityAsync(offer);
        var isOrder = offer.IsOrder;

        // Teklifi hazırlayan satışçının imza-kaşesi → "Firma Yetkilisi İmza" alanına.
        var firmaStamp = string.IsNullOrEmpty(offer.CreatedBy) ? null
            : await _db.Users.Where(u => u.Email == offer.CreatedBy)
                .Select(u => u.SignatureStamp).FirstOrDefaultAsync();

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
                    SectionTable(col, "Kombi / Kazan", offer, OfferSections.KombiKazan, includeLinePrices);
                    SectionTable(col, "Doğalgaz Tesisatı", offer, OfferSections.GasInstallation, includeLinePrices);
                    SectionTable(col, "Malzeme / Ekipman", offer, OfferSections.Material, includeLinePrices);
                    RadiatorTable(col, offer, includeLinePrices);
                    SectionTable(col, "Tesisat Hizmetleri", offer, OfferSections.Installation, includeLinePrices);
                    SectionTable(col, "İşçilik", offer, OfferSections.Labor, includeLinePrices);
                    if (!string.IsNullOrWhiteSpace(offer.GeneralNotes))
                        NotesBlock(col, offer.GeneralNotes!);
                    PricingSummary(col, offer, includeLinePrices);
                    if (includeLinePrices) PaymentPlanBlock(col, offer);
                    WorkProgramBlock(col, offer, includeLinePrices);
                    // Eksik ürünler yalnızca iç (fiyatlı) PDF'te; müşteri PDF'inde gösterilmez.
                    if (includeLinePrices && summary.InsufficientItems.Count > 0)
                        MissingProductsBlock(col, summary.InsufficientItems);
                    SignatureBlock(col, offer.CustomerSignature, firmaStamp);
                });

                page.Footer().Element(CompanyFooter);
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

    private void SectionTable(ColumnDescriptor col, string title, Offer offer, string section, bool prices)
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
                if (prices)
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(20); c.RelativeColumn(3); c.ConstantColumn(45);
                        c.ConstantColumn(40); c.ConstantColumn(60); c.ConstantColumn(65); c.ConstantColumn(70);
                    });
                    TableHeader(table, "#", "Kalem", "Adet", "Birim", "B.Fiyat", "Toplam", "Stok");
                }
                else
                {
                    // Müşteri PDF'i: fiyat/stok sütunları yok.
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(20); c.RelativeColumn(4); c.ConstantColumn(55); c.ConstantColumn(50);
                    });
                    TableHeader(table, "#", "Kalem", "Adet", "Birim");
                }

                var i = 1;
                foreach (var it in items)
                {
                    // Müşteri PDF'inde stok/kırmızı bilgisi de gizlidir.
                    var bg = (prices && it.IsStockInsufficient) ? Red : (i % 2 == 0 ? LightGray : "#ffffff");
                    var fc = (prices && it.IsStockInsufficient) ? "#ffffff" : "#000000";
                    Cell(table, bg, fc, (i++).ToString());
                    Cell(table, bg, fc, it.ItemName);
                    Cell(table, bg, fc, Num(it.Quantity));
                    Cell(table, bg, fc, it.Unit);
                    if (prices)
                    {
                        Cell(table, bg, fc, Money(it.UnitPrice));
                        Cell(table, bg, fc, Money(it.TotalPrice));
                        Cell(table, bg, fc, it.IsStockInsufficient ? $"Eksik {Num(it.MissingQuantity)}" : "-");
                    }
                }
            });
        });
    }

    private void RadiatorTable(ColumnDescriptor col, Offer offer, bool prices)
    {
        var radItems = offer.RadiatorItems.Where(r => r.Quantity > 0 || r.TotalPrice > 0).ToList();
        if (radItems.Count == 0) return;
        Card(col, "Radyatör & Vana", inner =>
        {
            inner.Item().Table(table =>
            {
                if (prices)
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4); c.ConstantColumn(55); c.ConstantColumn(45);
                        c.ConstantColumn(65); c.ConstantColumn(70);
                    });
                    TableHeader(table, "Kalem", "Panel(m)", "Adet", "B.Fiyat", "Toplam");
                }
                else
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(4); c.ConstantColumn(55); c.ConstantColumn(45); });
                    TableHeader(table, "Kalem", "Panel(m)", "Adet");
                }
                var i = 0;
                foreach (var r in radItems)
                {
                    var bg = i++ % 2 == 0 ? LightGray : "#ffffff";
                    var kalem = r.IsValve
                        ? (r.ItemName ?? "Vana")
                        : $"{r.RoomName} {r.RadiatorBrand} {r.RadiatorHeight}*{r.RadiatorWidth}".Trim();
                    Cell(table, bg, "#000000", kalem);
                    Cell(table, bg, "#000000", r.IsValve ? "-" : Num(r.PanelLength));
                    Cell(table, bg, "#000000", Num(r.Quantity));
                    if (prices)
                    {
                        Cell(table, bg, "#000000", Money(r.UnitPrice));
                        Cell(table, bg, "#000000", Money(r.TotalPrice));
                    }
                }
            });
            var totalPanel = radItems.Where(r => !r.IsValve).Sum(r => r.PanelLength * (r.Quantity > 0 ? r.Quantity : 1));
            inner.Item().PaddingTop(3).AlignRight().Text($"Toplam panel uzunluğu: {Num(totalPanel)} m").Italic();
        });
    }

    private void PricingSummary(ColumnDescriptor col, Offer offer, bool prices)
    {
        Card(col, prices ? "Fiyatlandırma Özeti" : "Tutar", inner =>
        {
            inner.Item().Row(r =>
            {
                r.RelativeItem();
                r.ConstantItem(240).Column(c =>
                {
                    if (prices)
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
                    }
                    c.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("GENEL TOPLAM").Bold().FontColor(Navy);
                        rr.ConstantItem(90).AlignRight().Text(Money(offer.GrandTotal)).Bold().FontColor(Navy);
                    });
                    if (prices)
                    {
                        Line(c, "Peşinat", offer.AdvancePayment);
                        Line(c, "Kalan Ödeme", offer.RemainingPayment);
                    }
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

    private void WorkProgramBlock(ColumnDescriptor col, Offer offer, bool prices)
    {
        Card(col, "İş Programı", inner =>
        {
            inner.Item().Row(r =>
            {
                r.RelativeItem().Text($"Başlangıç: {(offer.WorkStartDate.HasValue ? offer.WorkStartDate.Value.ToString("dd.MM.yyyy") : "-")}");
                r.RelativeItem().Text($"Teslim: {(offer.WorkEndDate.HasValue ? offer.WorkEndDate.Value.ToString("dd.MM.yyyy") : "-")}");
                // Usta ve hakediş iç bilgidir — yalnızca ofis (fiyatlı) PDF'inde görünür.
                if (prices)
                    r.RelativeItem().Text($"Usta: {offer.Usta?.Name ?? "-"}");
                else
                    r.RelativeItem().Text("");
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

    private void SignatureBlock(ColumnDescriptor col, string? customerSignature, string? firmaStamp)
    {
        var sigBytes = DecodeSignature(customerSignature);
        var stampBytes = DecodeSignature(firmaStamp);
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
                if (stampBytes is not null)
                    c.Item().Height(50).AlignCenter().Image(stampBytes).FitHeight();
                else
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
    /// <summary>PDF alt künyesi — firma iletişim bilgileri.</summary>
    private void CompanyFooter(IContainer f)
    {
        f.BorderTop(0.5f).BorderColor(Gray).PaddingTop(4).AlignCenter().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Gray));
            t.Span("Tel: 0 (242) 226 37 16");
            t.Span("   •   ");
            t.Span("No:18/B Konyaaltı / ANTALYA");
            t.Span("   •   ");
            t.Span("info@ozdemirmuhendislik.com");
        });
    }

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

    // ==================== SERVİS FORMU PDF ====================
    public async Task<(string FileName, byte[] Content)> GenerateServicePdfAsync(int serviceId)
    {
        var s = await _db.ServiceRecords
            .Include(x => x.Customer)
            .Include(x => x.ServicedProduct)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceId)
            ?? throw new InvalidOperationException("Servis kaydı bulunamadı.");

        byte[] logo = _logoPath is not null && File.Exists(_logoPath)
            ? await File.ReadAllBytesAsync(_logoPath) : Array.Empty<byte>();

        var custName = s.Customer?.FullName ?? s.CustomerName ?? "-";
        var phone = s.Customer?.Phone ?? s.Phone ?? "-";
        var address = s.Customer?.Address ?? s.Address ?? "-";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Helvetica"));

                page.Header().Row(row =>
                {
                    if (logo.Length > 0)
                        row.ConstantItem(150).AlignMiddle().Image(logo).FitWidth();
                    else
                        row.ConstantItem(150).AlignMiddle().Text("ÖZDEMİR").FontSize(20).Bold().FontColor(Navy);
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text("SERVİS FORMU").FontSize(16).Bold().FontColor(Navy);
                        c.Item().Text("Kombi / Klima / Isıtma Sistemleri").FontSize(9).FontColor(Gray);
                        c.Item().PaddingTop(4).Text($"Servis No: {s.ServiceNumber}").Bold();
                        c.Item().Text($"Başvuru: {s.ApplicationDate:dd.MM.yyyy}");
                    });
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(8);

                    Card(col, "Müşteri Bilgileri", inner =>
                    {
                        inner.Item().Text($"Ad Soyad: {custName}");
                        inner.Item().Text($"Telefon: {phone}");
                        inner.Item().Text($"Adres: {address}");
                    });

                    Card(col, "Servis Bilgileri", inner =>
                    {
                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Randevu: {(s.AppointmentDate.HasValue ? s.AppointmentDate.Value.ToString("dd.MM.yyyy") : "-")}");
                            r.RelativeItem().Text($"Onarım: {(s.RepairDate.HasValue ? s.RepairDate.Value.ToString("dd.MM.yyyy") : "-")}");
                        });
                        inner.Item().Text($"Servis Nedeni: {ServiceReasonLabels.Text(s.ServiceReasons)}");
                        var cihaz = s.ServicedProduct?.Name
                            ?? $"{s.DeviceBrand} {s.DeviceModel} {s.DeviceType}".Trim();
                        inner.Item().Text($"Cihaz: {(string.IsNullOrWhiteSpace(cihaz) ? "-" : cihaz)}");
                    });

                    Card(col, "Şikayetin Konusu", inner =>
                        inner.Item().Text(string.IsNullOrWhiteSpace(s.ComplaintSubject) ? "-" : s.ComplaintSubject));

                    Card(col, "Yapılan İşlem", inner =>
                        inner.Item().Text(string.IsNullOrWhiteSpace(s.WorkDone) ? "-" : s.WorkDone));

                    Card(col, "Ödenecek Genel Toplam", inner =>
                        inner.Item().AlignRight().Text(Money(s.TotalAmount)).Bold().FontColor(Navy).FontSize(12));

                    if (!string.IsNullOrWhiteSpace(s.SpecialNote))
                        Card(col, "Özel Not", inner => inner.Item().Text(s.SpecialNote!));

                    // İmzalar
                    var techSig = DecodeSignature(s.TechnicianSignature);
                    var custSig = DecodeSignature(s.CustomerSignature);
                    col.Item().PaddingTop(16).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            if (techSig is not null) c.Item().Height(50).AlignCenter().Image(techSig).FitHeight();
                            else c.Item().Height(50);
                            c.Item().LineHorizontal(0.7f);
                            c.Item().AlignCenter().Text($"Teknisyen{(string.IsNullOrWhiteSpace(s.TechnicianName) ? "" : " - " + s.TechnicianName)}").FontColor(Gray);
                        });
                        r.ConstantItem(40);
                        r.RelativeItem().Column(c =>
                        {
                            if (custSig is not null) c.Item().Height(50).AlignCenter().Image(custSig).FitHeight();
                            else c.Item().Height(50);
                            c.Item().LineHorizontal(0.7f);
                            c.Item().AlignCenter().Text("Müşteri İmza").FontColor(Gray);
                        });
                    });

                    col.Item().PaddingTop(8).Text(
                        "Cihaz üzerinde değiştirilen parçalar 1 (bir) yıl garanti kapsamındadır.")
                        .Italic().FontColor(Gray).FontSize(8);
                });

                page.Footer().Element(CompanyFooter);
            });
        });

        var bytes = doc.GeneratePdf();
        var safe = custName.Replace(" ", "_");
        return ($"Servis_Formu_{safe}_{s.ApplicationDate:yyyyMMdd}.pdf", bytes);
    }
}
