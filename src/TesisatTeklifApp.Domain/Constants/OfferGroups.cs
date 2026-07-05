namespace TesisatTeklifApp.Domain.Constants;

/// <summary>
/// Teklif formundaki ürün seçim grupları (Excel "Başlıklar" dosyasındaki 10 grup).
/// Her grup akordeon başlığı olur; altındaki ürünler seçilerek eklenir.
/// <see cref="Section"/> mevcut 6 bölüm toplamına (KombiKazanTotal vb.) map eder;
/// böylece OfferCalculationService değişmeden çalışır.
/// </summary>
public class OfferGroup
{
    public string Name { get; init; } = "";          // Akordeon başlığı (Excel grup adı)
    public string Section { get; init; } = "";        // OfferSections.* — toplam bölümü
    public string[] Categories { get; init; } = System.Array.Empty<string>();
    public bool IsRadiator { get; init; }             // Radyatör özel bölümü (ölçü/panel)
    public bool AllowFreeText { get; init; }          // Serbest kalem girişi (tesisat/işçilik)
    public string Icon { get; init; } = "bi-box";
}

public static class OfferGroups
{
    public static readonly IReadOnlyList<OfferGroup> All = new List<OfferGroup>
    {
        new() { Name = "KOMBİ / KAZAN", Section = OfferSections.KombiKazan, Icon = "bi-fire",
                Categories = new[] { ProductCategories.Kombi, ProductCategories.Kazan } },
        new() { Name = "KLİMA", Section = OfferSections.KombiKazan, Icon = "bi-snow",
                Categories = new[] { ProductCategories.Klima }, AllowFreeText = true },
        new() { Name = "ISI POMPASI", Section = OfferSections.KombiKazan, Icon = "bi-thermometer-half",
                Categories = new[] { ProductCategories.IsiPompasi } },
        new() { Name = "RADYATÖR", Section = OfferSections.Material, Icon = "bi-grid-3x3-gap",
                Categories = new[] { ProductCategories.Radyator }, IsRadiator = true },
        new() { Name = "DOĞALGAZ TESİSATI", Section = OfferSections.GasInstallation, Icon = "bi-diagram-3",
                Categories = new[] { ProductCategories.DogalgazTesisati, ProductCategories.DogalgazBorulari,
                    ProductCategories.DogalgazVanalari, ProductCategories.FittingsMalzemeler,
                    ProductCategories.PlastikBorular }, AllowFreeText = true },
        new() { Name = "KOLON TESİSATI", Section = OfferSections.GasInstallation, Icon = "bi-building",
                Categories = new[] { ProductCategories.KolonTesisati }, AllowFreeText = true },
        new() { Name = "KALORİFER TESİSATI", Section = OfferSections.Installation, Icon = "bi-thermometer",
                Categories = new[] { ProductCategories.KaloriferTesisati }, AllowFreeText = true },
        new() { Name = "MALZEME / EKİPMAN", Section = OfferSections.Material, Icon = "bi-tools",
                Categories = new[] { ProductCategories.MalzemeEkipman, ProductCategories.RadyatorVanasi,
                    ProductCategories.TermostatikVana, ProductCategories.KombiDolabi, ProductCategories.OdaTermostati,
                    ProductCategories.BacaUzatmasi, ProductCategories.SelenoidVana, ProductCategories.GazAlarmCihazi,
                    ProductCategories.CamMenfezi, ProductCategories.KombiElektrikIsi, ProductCategories.Diger } },
        new() { Name = "KLİMA EK HİZMET & MALZEME", Section = OfferSections.Material, Icon = "bi-wind",
                Categories = new[] { ProductCategories.KlimaHizmet }, AllowFreeText = true },
        new() { Name = "İŞÇİLİK", Section = OfferSections.Labor, Icon = "bi-hammer",
                Categories = new[] { ProductCategories.Iscilik, ProductCategories.Montaj,
                    ProductCategories.FullTesisat, ProductCategories.HatTasima, ProductCategories.EkHatAlma },
                AllowFreeText = true },
    };

    /// <summary>Bir ürün kategorisinin ait olduğu ilk grup (yoksa null).</summary>
    public static OfferGroup? ForCategory(string? category) =>
        string.IsNullOrEmpty(category) ? null
        : All.FirstOrDefault(g => g.Categories.Contains(category));
}
