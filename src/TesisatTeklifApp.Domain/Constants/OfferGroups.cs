namespace TesisatTeklifApp.Domain.Constants;

/// <summary>
/// Teklif formundaki ürün seçim grupları — firmanın "Başlıklar" Excel'indeki 8 grup.
/// Gruba tıklayınca kalemleri (birimleriyle) listelenir; "Grubu Ekle" ile toplu eklenebilir.
/// <see cref="Section"/> mevcut 6 bölüm toplamına map eder; OfferCalculationService değişmez.
/// </summary>
public class OfferGroup
{
    public string Name { get; init; } = "";
    public string Section { get; init; } = "";
    public string[] Categories { get; init; } = System.Array.Empty<string>();
    public bool IsRadiator { get; init; }
    public bool AllowFreeText { get; init; }
    public string Icon { get; init; } = "bi-box";
}

public static class OfferGroups
{
    public static readonly IReadOnlyList<OfferGroup> All = new List<OfferGroup>
    {
        new() { Name = "KOMBİ KLİMA ISI POMPASI DİĞER", Section = OfferSections.KombiKazan, Icon = "bi-fire",
                Categories = new[] { ProductCategories.Kombi, ProductCategories.Kazan,
                    ProductCategories.Klima, ProductCategories.IsiPompasi, ProductCategories.Diger } },

        new() { Name = "RADYATÖR MARKASI", Section = OfferSections.Material, Icon = "bi-grid-3x3-gap",
                Categories = new[] { ProductCategories.Radyator }, IsRadiator = true },

        new() { Name = "DOĞALGAZ TESİSATI", Section = OfferSections.GasInstallation, Icon = "bi-diagram-3",
                Categories = new[] { ProductCategories.DogalgazTesisati }, AllowFreeText = true },

        new() { Name = "KOLON TESİSATI", Section = OfferSections.GasInstallation, Icon = "bi-building",
                Categories = new[] { ProductCategories.KolonTesisati }, AllowFreeText = true },

        new() { Name = "KAROİFER TESİSATI GRUPLARI", Section = OfferSections.Installation, Icon = "bi-thermometer",
                Categories = new[] { ProductCategories.KaloriferTesisati }, AllowFreeText = true },

        // Vana kategorileri burada YOK: vanalar radyatör bölümündeki kendi alanından eklenir.
        // (Aksi halde aynı vana hem buradan hem radyatörden eklenip iki kez sayılabiliyordu.)
        new() { Name = "MALZEME EKİPMAN", Section = OfferSections.Material, Icon = "bi-tools",
                Categories = new[] { ProductCategories.MalzemeEkipman } },

        new() { Name = "İŞÇİLİKLER-DOĞALGAZ", Section = OfferSections.Labor, Icon = "bi-hammer",
                Categories = new[] { ProductCategories.Iscilik }, AllowFreeText = true },

        new() { Name = "KLİMA EK HİZMETLER VE MALZEMELER", Section = OfferSections.Installation, Icon = "bi-wind",
                Categories = new[] { ProductCategories.KlimaHizmet }, AllowFreeText = true },
    };

    /// <summary>Radyatör dışı vana kategorileri (Vana Ekle listesi).</summary>
    public static readonly string[] ValveCategories =
        { ProductCategories.RadyatorVanasi, ProductCategories.TermostatikVana };

    public static OfferGroup? ForCategory(string? category) =>
        string.IsNullOrEmpty(category) ? null
        : All.FirstOrDefault(g => g.Categories.Contains(category));
}
