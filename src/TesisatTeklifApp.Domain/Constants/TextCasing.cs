using System.Globalization;

namespace TesisatTeklifApp.Domain.Constants;

/// <summary>
/// Metin büyük harf dönüşümü — Türkçe kurallarıyla (i→İ, ı→I).
/// Girilen tüm serbest metinler veritabanına büyük harfle yazılır; aramada da
/// aynı dönüşüm uygulanmalıdır (Postgres'te karşılaştırma harf duyarlıdır).
/// </summary>
public static class TextCasing
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Türkçe büyük harfe çevirir. Boş/boşluk ise olduğu gibi döner.</summary>
    public static string? TrUpper(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : value.ToUpper(Tr);

    /// <summary>
    /// Entity tipi → büyük harfe çevrilecek property adları.
    /// Sadece burada listelenen alanlar dönüştürülür (opt-in). E-posta, base64 imza/kaşe,
    /// token, üretilen numaralar ve sabitle eşleşen alanlar (Kategori, Birim, GroupName,
    /// SectionName) bilerek listede yoktur — dönüştürülürse eşleşmeler bozulur.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> UpperFields =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Customer"] = new[] { "FirstName", "LastName", "Address", "City", "District", "Description" },
            ["Product"] = new[] { "Name", "Brand", "Model", "Description" },
            ["Supplier"] = new[] { "Name", "TaxNumber", "Address", "City", "District", "Description" },
            ["Usta"] = new[] { "Name", "Specialty" },
            ["ServiceRecord"] = new[]
            {
                "CustomerName", "Address", "DeviceBrand", "DeviceModel", "DeviceType",
                "ComplaintSubject", "WorkDone", "SpecialNote", "TechnicianName"
            },
            ["OfferItem"] = new[] { "ItemName", "Description" },
            ["RadiatorItem"] = new[] { "ItemName", "RoomName", "RadiatorBrand", "RadiatorSize", "Description" },
            ["Offer"] = new[] { "GeneralNotes", "ResponsiblePerson", "EmployerName" },
            ["PaymentPlan"] = new[] { "Description" },
            ["PurchaseOrder"] = new[] { "Notes" },
            ["PurchaseOrderItem"] = new[] { "ItemName" },
            ["UstaPayment"] = new[] { "Description" },
        };
}
