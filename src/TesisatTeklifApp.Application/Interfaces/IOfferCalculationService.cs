using TesisatTeklifApp.Domain.Entities;

namespace TesisatTeklifApp.Application.Interfaces;

/// <summary>
/// Tüm fiyat hesaplamalarını yapan saf servis (veritabanı bağımlılığı yoktur).
/// Controller / Razor component içinde hesaplama YAPILMAZ; bu servis çağrılır.
/// </summary>
public interface IOfferCalculationService
{
    decimal CalculateItemTotal(OfferItem item);
    decimal CalculateRadiatorTotal(RadiatorItem item);

    decimal CalculateKombiKazanTotal(Offer offer);
    decimal CalculateGasInstallationTotal(Offer offer);
    decimal CalculateMaterialTotal(Offer offer);
    decimal CalculateRadiatorSectionTotal(Offer offer);
    decimal CalculateInstallationTotal(Offer offer);
    decimal CalculateLaborTotal(Offer offer);

    decimal CalculateSubTotal(Offer offer);
    decimal CalculateDiscount(decimal subTotal, decimal discountRate);
    decimal CalculateVat(decimal baseAmount, decimal vatRate);
    decimal CalculateGrandTotal(Offer offer);
    decimal CalculateRemainingPayment(decimal grandTotal, decimal advancePayment);

    /// <summary>
    /// Teklifin tüm satır ve bölüm toplamlarını yeniden hesaplayıp Offer üzerine yazar.
    /// Form üzerinde her değişiklikte çağrılır.
    /// </summary>
    void RecalculateOfferTotals(Offer offer);
}
