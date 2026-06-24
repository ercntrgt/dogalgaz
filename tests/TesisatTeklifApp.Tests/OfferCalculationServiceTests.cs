using TesisatTeklifApp.Application.Services;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class OfferCalculationServiceTests
{
    private readonly OfferCalculationService _calc = new();

    private static OfferItem Item(string section, decimal qty, decimal price, bool selected = true) =>
        new() { SectionName = section, Quantity = qty, UnitPrice = price, IsSelected = selected };

    [Fact]
    public void ItemTotal_Is_Quantity_Times_Price()
    {
        Assert.Equal(32000m, _calc.CalculateItemTotal(Item(OfferSections.KombiKazan, 1, 32000m)));
        Assert.Equal(5000m, _calc.CalculateItemTotal(Item(OfferSections.Labor, 2, 2500m)));
    }

    [Fact]
    public void Unselected_Material_Item_Counts_As_Zero()
    {
        var item = Item(OfferSections.Material, 3, 450m, selected: false);
        Assert.Equal(0m, _calc.CalculateItemTotal(item));
    }

    [Fact]
    public void RadiatorTotal_Is_Panel_Plus_Valve()
    {
        var r = new RadiatorItem { PanelLength = 4m, MeterPrice = 2500m, ValveQuantity = 2m, ValveUnitPrice = 450m };
        // 4*2500 + 2*450 = 10000 + 900 = 10900
        Assert.Equal(10900m, _calc.CalculateRadiatorTotal(r));
    }

    [Fact]
    public void Recalculate_Vat_Excluded_Adds_Vat_On_Top()
    {
        var offer = new Offer { VatRate = 20m, IsVatIncluded = false };
        offer.Items.Add(Item(OfferSections.KombiKazan, 1, 1000m));
        _calc.RecalculateOfferTotals(offer);

        Assert.Equal(1000m, offer.SubTotal);
        Assert.Equal(0m, offer.DiscountAmount);
        Assert.Equal(200m, offer.VatAmount);     // 1000 * 20%
        Assert.Equal(1200m, offer.GrandTotal);
    }

    [Fact]
    public void Recalculate_With_Discount_Applies_Before_Vat()
    {
        var offer = new Offer { VatRate = 20m, IsVatIncluded = false, DiscountRate = 10m };
        offer.Items.Add(Item(OfferSections.Labor, 1, 1000m));
        _calc.RecalculateOfferTotals(offer);

        Assert.Equal(100m, offer.DiscountAmount);  // 10% of 1000
        Assert.Equal(180m, offer.VatAmount);       // (1000-100)*20%
        Assert.Equal(1080m, offer.GrandTotal);     // 900 + 180
    }

    [Fact]
    public void Recalculate_Vat_Included_Extracts_Vat_From_Total()
    {
        var offer = new Offer { VatRate = 20m, IsVatIncluded = true };
        offer.Items.Add(Item(OfferSections.KombiKazan, 1, 1200m));  // KDV dahil
        _calc.RecalculateOfferTotals(offer);

        Assert.Equal(1200m, offer.GrandTotal);     // toplam değişmez
        Assert.Equal(200m, offer.VatAmount);       // 1200 - (1200/1.2)=1000 -> 200
    }

    [Fact]
    public void RemainingPayment_Is_GrandTotal_Minus_Advance()
    {
        var offer = new Offer { VatRate = 0m, IsVatIncluded = false, AdvancePayment = 300m };
        offer.Items.Add(Item(OfferSections.Labor, 1, 1000m));
        _calc.RecalculateOfferTotals(offer);

        Assert.Equal(1000m, offer.GrandTotal);
        Assert.Equal(700m, offer.RemainingPayment);
    }

    [Fact]
    public void SubTotal_Sums_All_Sections_Including_Radiator()
    {
        var offer = new Offer { VatRate = 0m };
        offer.Items.Add(Item(OfferSections.KombiKazan, 1, 1000m));
        offer.Items.Add(Item(OfferSections.GasInstallation, 2, 100m));   // 200
        offer.Items.Add(Item(OfferSections.Material, 1, 450m, selected: true)); // 450
        offer.Items.Add(Item(OfferSections.Installation, 1, 5000m));     // 5000
        offer.Items.Add(Item(OfferSections.Labor, 1, 3000m));           // 3000
        offer.RadiatorItems.Add(new RadiatorItem { PanelLength = 2m, MeterPrice = 2500m }); // 5000
        _calc.RecalculateOfferTotals(offer);

        // 1000 + 200 + 450 + 5000 + 3000 + 5000 = 14650
        Assert.Equal(14650m, offer.SubTotal);
    }
}
