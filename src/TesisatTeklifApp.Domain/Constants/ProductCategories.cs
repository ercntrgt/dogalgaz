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
    public const string Klima = "Klima";
    public const string IsiPompasi = "Isı Pompası";
    public const string MalzemeEkipman = "Malzeme/Ekipman";
    public const string DogalgazTesisati = "Doğalgaz Tesisatı";
    public const string KolonTesisati = "Kolon Tesisatı";
    public const string KaloriferTesisati = "Kalorifer Tesisatı";
    public const string KlimaHizmet = "Klima Hizmet/Malzeme";
    public const string Diger = "Diğer";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Kombi, Kazan, Klima, Radyator, RadyatorVanasi, TermostatikVana, KombiDolabi,
        OdaTermostati, BacaUzatmasi, KombiElektrikIsi, SelenoidVana, GazAlarmCihazi,
        CamMenfezi, DogalgazBorulari, DogalgazVanalari, DogalgazTesisati, KolonTesisati,
        KaloriferTesisati, FittingsMalzemeler, PlastikBorular, MalzemeEkipman,
        Iscilik, Montaj, FullTesisat, HatTasima, EkHatAlma, KlimaHizmet, Diger
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

/// <summary>
/// Teklif formunda her bölümün ürün açılır listesinde hangi kategorilerin
/// görüneceğini belirler (alan bazlı filtre). Eşleşme yoksa tüm ürünler gösterilir.
/// Yeni kategori eklendiğinde ilgili bölüme buradan eklenir.
/// </summary>
public static class SectionCategories
{
    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        [OfferSections.KombiKazan] = new[]
        {
            ProductCategories.Kombi, ProductCategories.Kazan,
            ProductCategories.Klima, ProductCategories.IsiPompasi
        },
        [OfferSections.GasInstallation] = new[]
        {
            ProductCategories.DogalgazTesisati, ProductCategories.DogalgazBorulari,
            ProductCategories.DogalgazVanalari, ProductCategories.KolonTesisati,
            ProductCategories.FittingsMalzemeler, ProductCategories.PlastikBorular
        },
        [OfferSections.Material] = new[]
        {
            ProductCategories.MalzemeEkipman, ProductCategories.RadyatorVanasi,
            ProductCategories.TermostatikVana, ProductCategories.KombiDolabi,
            ProductCategories.OdaTermostati, ProductCategories.BacaUzatmasi,
            ProductCategories.SelenoidVana, ProductCategories.GazAlarmCihazi,
            ProductCategories.CamMenfezi, ProductCategories.KombiElektrikIsi,
            ProductCategories.FittingsMalzemeler, ProductCategories.Diger
        },
        [OfferSections.Installation] = new[]
        {
            ProductCategories.Montaj, ProductCategories.FullTesisat,
            ProductCategories.HatTasima, ProductCategories.EkHatAlma,
            ProductCategories.KaloriferTesisati, ProductCategories.KlimaHizmet
        },
        [OfferSections.Labor] = new[] { ProductCategories.Iscilik },
    };

    /// <summary>Verilen bölüm için izin verilen kategoriler; yoksa null (=tümü).</summary>
    public static string[]? For(string section) =>
        Map.TryGetValue(section, out var cats) ? cats : null;
}

/// <summary>Ürün/kalem birimleri (açılır menüde kullanılır).</summary>
public static class Units
{
    public const string Adet = "Adet";
    public const string Metre = "Metre";

    public static readonly IReadOnlyList<string> All = new[]
    {
        "Adet", "Metre", "m²", "m³", "Takım", "Set", "Paket", "Kutu", "Rulo",
        "Kg", "Litre", "Saat", "Gün", "Boy", "Top"
    };
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
