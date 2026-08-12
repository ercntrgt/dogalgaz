using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;

namespace TesisatTeklifApp.Application.Services;

/// <summary>
/// Saf hesaplama servisi. Hiçbir DB/IO bağımlılığı yoktur, kolayca test edilebilir.
/// </summary>
public class OfferCalculationService : IOfferCalculationService
{
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>KalemToplam = Adet x BirimFiyat. Seçili olmayan (D bölümü) kalem 0 sayılır.</summary>
    public decimal CalculateItemTotal(OfferItem item)
    {
        if (!item.IsSelected)
            return 0m;
        return Round(item.Quantity * item.UnitPrice);
    }

    /// <summary>SatırToplam = Adet × BirimFiyat (hem panel hem vana için ortak).</summary>
    public decimal CalculateRadiatorTotal(RadiatorItem item) => Round(item.Quantity * item.UnitPrice);

    public decimal CalculateKombiKazanTotal(Offer offer) => SectionTotal(offer, OfferSections.KombiKazan);
    public decimal CalculateGasInstallationTotal(Offer offer) => SectionTotal(offer, OfferSections.GasInstallation);
    public decimal CalculateMaterialTotal(Offer offer) => SectionTotal(offer, OfferSections.Material);
    public decimal CalculateInstallationTotal(Offer offer) => SectionTotal(offer, OfferSections.Installation);
    public decimal CalculateLaborTotal(Offer offer) => SectionTotal(offer, OfferSections.Labor);

    public decimal CalculateRadiatorSectionTotal(Offer offer)
        => Round(offer.RadiatorItems.Sum(CalculateRadiatorTotal));

    private decimal SectionTotal(Offer offer, string section)
        => Round(offer.Items.Where(i => i.SectionName == section).Sum(CalculateItemTotal));

    /// <summary>AraToplam = tüm bölüm toplamlarının toplamı.</summary>
    public decimal CalculateSubTotal(Offer offer)
        => Round(offer.KombiKazanTotal + offer.GasInstallationTotal + offer.MaterialTotal
                 + offer.RadiatorTotal + offer.InstallationTotal + offer.LaborTotal);

    public decimal CalculateDiscount(decimal subTotal, decimal discountRate)
        => Round(subTotal * discountRate / 100m);

    public decimal CalculateVat(decimal baseAmount, decimal vatRate)
        => Round(baseAmount * vatRate / 100m);

    /// <summary>KDV dahil bir tutarın içindeki KDV'yi ayrıştırır (matrah = tutar / (1 + oran/100)).</summary>
    private static decimal ExtractVat(decimal grossAmount, decimal vatRate)
    {
        var divisor = 1 + vatRate / 100m;
        if (divisor <= 0) return 0m;
        return Round(grossAmount - Round(grossAmount / divisor));
    }

    /// <summary>Elle düzeltme uygulanmadan önceki, kalemlerden hesaplanan genel toplam.</summary>
    public decimal CalculateAutoGrandTotal(Offer offer)
    {
        var afterDiscount = Round(offer.SubTotal - offer.DiscountAmount);
        return offer.IsVatIncluded
            ? afterDiscount                                                  // fiyatlar KDV dahil girilmiş
            : Round(afterDiscount + CalculateVat(afterDiscount, offer.VatRate));
    }

    public decimal CalculateGrandTotal(Offer offer) => offer.GrandTotal;

    public decimal CalculateRemainingPayment(decimal grandTotal, decimal advancePayment)
        => Round(grandTotal - advancePayment);

    /// <summary>
    /// Satır toplamlarını, bölüm toplamlarını, iskonto/KDV ve genel toplamı hesaplar.
    /// KDV dahil seçili ise KDV, toplam içinden ayrıştırılır (matrah = tutar / (1 + oran/100)).
    /// </summary>
    public void RecalculateOfferTotals(Offer offer)
    {
        // 1) Satır toplamları
        foreach (var item in offer.Items)
            item.TotalPrice = CalculateItemTotal(item);
        foreach (var rad in offer.RadiatorItems)
            rad.TotalPrice = CalculateRadiatorTotal(rad);

        // 2) Bölüm toplamları
        offer.KombiKazanTotal = CalculateKombiKazanTotal(offer);
        offer.GasInstallationTotal = CalculateGasInstallationTotal(offer);
        offer.MaterialTotal = CalculateMaterialTotal(offer);
        offer.RadiatorTotal = CalculateRadiatorSectionTotal(offer);
        offer.InstallationTotal = CalculateInstallationTotal(offer);
        offer.LaborTotal = CalculateLaborTotal(offer);

        // 3) Ara toplam ve iskonto
        offer.SubTotal = CalculateSubTotal(offer);
        offer.DiscountAmount = CalculateDiscount(offer.SubTotal, offer.DiscountRate);
        var afterDiscount = Round(offer.SubTotal - offer.DiscountAmount);

        // 4) KDV ve genel toplam
        offer.VatAmount = offer.IsVatIncluded
            ? ExtractVat(afterDiscount, offer.VatRate)                 // fiyatlar KDV dahil → içinden ayrıştır
            : CalculateVat(afterDiscount, offer.VatRate);              // fiyatlar KDV hariç → üstüne ekle
        offer.GrandTotal = CalculateAutoGrandTotal(offer);

        // 4b) Genel toplam elle girildiyse (yuvarlama/pazarlık) hesaplanan tutarın yerine geçer.
        // Ek oran tutarı da girilen toplamın içinden ayrıştırılır ki özet kendi içinde tutarlı kalsın.
        if (offer.GrandTotalOverride is decimal manual)
        {
            offer.GrandTotal = Round(manual);
            offer.VatAmount = ExtractVat(offer.GrandTotal, offer.VatRate);
        }

        // 5) Kalan ödeme
        offer.RemainingPayment = CalculateRemainingPayment(offer.GrandTotal, offer.AdvancePayment);
    }
}
