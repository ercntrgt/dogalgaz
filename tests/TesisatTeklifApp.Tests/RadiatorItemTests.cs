using TesisatTeklifApp.Domain.Entities;
using Xunit;

namespace TesisatTeklifApp.Tests;

public class RadiatorItemTests
{
    [Fact]
    public void Havlupan_without_mm_dims_shows_product_name()
    {
        var r = new RadiatorItem { ItemName = "HAVLUPAN 5*100 BEYAZ", RadiatorBrand = "HAVLUPAN", RoomName = "Banyo" };
        Assert.Equal("Banyo HAVLUPAN 5*100 BEYAZ", r.GetDisplayName());
    }

    [Fact]
    public void Panel_with_dims_shows_brand_and_size()
    {
        var r = new RadiatorItem { ItemName = "DEMİRDÖKÜM 600*1400", RadiatorBrand = "DEMİRDÖKÜM", RadiatorHeight = 600, RadiatorWidth = 1400, RoomName = "Salon" };
        Assert.Equal("Salon DEMİRDÖKÜM 600*1400", r.GetDisplayName());
    }

    [Fact]
    public void Valve_shows_item_name()
    {
        var r = new RadiatorItem { IsValve = true, ItemName = "TERMOSTATİK VANA" };
        Assert.Equal("TERMOSTATİK VANA", r.GetDisplayName());
    }
}
