using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Genel stok davranış ayarları. Tek satır olarak tutulur (Id=1).
/// </summary>
public class StockSettings : BaseEntity
{
    public StockDeductionMode StockDeductionMode { get; set; } = StockDeductionMode.DeductOnApproval;

    /// <summary>Stok eksiye düşebilsin mi? Varsayılan false.</summary>
    public bool AllowNegativeStock { get; set; }

    /// <summary>Teklif ekranında bilgilendirme amaçlı stok uyarısı gösterilsin mi?</summary>
    public bool ShowStockWarningOnOffer { get; set; } = true;

    /// <summary>Uygulanmış katalog sürümü. catalog.json güncellenince artırılır; seeder yeni kalemleri bir kez ekler.</summary>
    public int CatalogVersion { get; set; }

    /// <summary>Uygulanmış metin normalizasyon sürümü (mevcut kayıtları büyük harfe çeviren tek seferlik geçiş).</summary>
    public int DataNormalizationVersion { get; set; }
}
