namespace TesisatTeklifApp.Domain.Constants;

/// <summary>
/// Ürün/malzeme kategorileri. Sabit liste olarak tutulur; ürün tanımlanırken
/// bu listeden seçilir. Yeni kategori eklemek için buraya eklenir.
/// </summary>
public static class ProductCategories
{
    public const string Kombi = "Kombi";
    public const string Kazan = "Kazan";
    public const string Radyator = "Radyatör";
    public const string RadyatorVanasi = "Radyatör vanası";
    public const string TermostatikVana = "Termostatik vana";
    public const string KombiDolabi = "Kombi dolabı";
    public const string OdaTermostati = "Oda termostatı";
    public const string BacaUzatmasi = "Baca uzatması";
    public const string KombiElektrikIsi = "Kombi elektrik işi";
    public const string SelenoidVana = "Selenoid vana";
    public const string GazAlarmCihazi = "Gaz alarm cihazı";
    public const string CamMenfezi = "Cam menfezi";
    public const string DogalgazBorulari = "Doğalgaz boruları";
    public const string DogalgazVanalari = "Doğalgaz vanaları";
    public const string FittingsMalzemeler = "Fittings malzemeler";
    public const string PlastikBorular = "Plastik borular";
    public const string Iscilik = "İşçilik";
    public const string Montaj = "Montaj";
    public const string FullTesisat = "Full tesisat";
    public const string HatTasima = "Hat taşıma";
    public const string EkHatAlma = "Ek hat alma";
    public const string Diger = "Diğer";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Kombi, Kazan, Radyator, RadyatorVanasi, TermostatikVana, KombiDolabi,
        OdaTermostati, BacaUzatmasi, KombiElektrikIsi, SelenoidVana, GazAlarmCihazi,
        CamMenfezi, DogalgazBorulari, DogalgazVanalari, FittingsMalzemeler,
        PlastikBorular, Iscilik, Montaj, FullTesisat, HatTasima, EkHatAlma, Diger
    };
}

/// <summary>
/// Teklif formundaki sabit bölüm adları. OfferItem.SectionName için kullanılır.
/// </summary>
public static class OfferSections
{
    public const string KombiKazan = "KombiKazan";        // B bölümü
    public const string GasInstallation = "GasInstallation"; // C bölümü
    public const string Material = "Material";             // D bölümü
    public const string Installation = "Installation";     // F bölümü
    public const string Labor = "Labor";                   // İşçilik
}

/// <summary>Radyatör bölümü için hazır oda isimleri.</summary>
public static class RoomNames
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Salon", "Yatak odası", "Oturma odası", "Çocuk odası", "Mutfak",
        "Banyo", "Hol", "Oda-1", "Oda-2", "Oda-3"
    };
}
