using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.Interfaces;

public interface IProductService
{
    Task<List<Product>> SearchAsync(ProductSearchFilter filter);
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetByCategoryAsync(string category);
    Task<List<Product>> GetActiveAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task SoftDeleteAsync(int id);
    Task AdjustStockAsync(int productId, decimal newQuantity, string? note);
}

public interface ICustomerService
{
    Task<List<Customer>> SearchAsync(CustomerSearchFilter filter);
    Task<Customer?> GetByIdAsync(int id);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task SoftDeleteAsync(int id);
}

public interface IOfferService
{
    Task<List<Offer>> SearchAsync(OfferSearchFilter filter);
    Task<Offer?> GetByIdAsync(int id);          // tüm satırlarıyla
    Task<Offer> CreateDraftAsync(int customerId, string? responsiblePerson);
    Task SaveAsync(Offer offer);                // ekle/güncelle + yeniden hesapla
    Task<Offer> CopyAsync(int offerId);         // teklif kopyalama
    Task ChangeStatusAsync(int offerId, OfferStatus status);
    Task<Offer> ConvertToOrderAsync(int offerId);   // siparişe dönüştür (+ stok kontrol)
    Task CancelAsync(int offerId);              // iptal + stok iadesi
}

public interface INumberGeneratorService
{
    Task<string> GenerateOfferNumberAsync();    // TSF-2026-0001
    Task<string> GenerateOrderNumberAsync();     // SPR-2026-0001
}

public interface IStockControlService
{
    /// <summary>Teklifteki tüm kalemler için stok durumunu hesaplar (DB'yi değiştirmez).</summary>
    Task<OfferStockSummary> CheckOfferStockAvailabilityAsync(Offer offer);

    void CheckOfferItemStockAvailability(OfferItem item, Product? product);
    void CheckRadiatorStockAvailability(RadiatorItem item, Product? radiatorProduct);

    /// <summary>Yeterli stoklu kalemleri stoktan düşer; yetersizleri WaitingSupply'a alır.</summary>
    Task<StockDeductionResult> DeductStockForOfferAsync(int offerId);

    Task ReserveStockForOfferAsync(int offerId);
    Task<List<InsufficientStockRow>> GetInsufficientStockItemsAsync(int offerId);
    Task<List<Product>> GetCriticalStockItemsAsync();
    Task CreateStockMovementAsync(int productId, int? offerId, MovementType type, decimal quantity, string? note);

    /// <summary>İptal edilen siparişte daha önce düşülen stoğu geri ekler.</summary>
    Task RestoreStockForCancelledOrderAsync(int offerId);
}

public interface IPdfExportService
{
    /// <summary>Teklif/sipariş PDF'i üretir. Dönen: (dosyaAdı, içerik).</summary>
    Task<(string FileName, byte[] Content)> GenerateOfferPdfAsync(int offerId);
}

public interface IExcelExportService
{
    Task<(string FileName, byte[] Content)> ExportOffersAsync(OfferSearchFilter filter);
    byte[] ExportRows<T>(IEnumerable<T> rows, string sheetName);
    byte[] ExportTable(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, string sheetName);
}

public interface IReportService
{
    Task<List<Offer>> GetOffersAsync(OfferSearchFilter filter);
    Task<List<InsufficientStockRow>> GetInsufficientStockReportAsync(ReportFilter filter);
    Task<List<Product>> GetCriticalStockReportAsync(ReportFilter filter);
    Task<List<RevenueReportRow>> GetRevenueReportAsync(ReportFilter filter);
    Task<List<ProductSalesRow>> GetProductSalesReportAsync(ReportFilter filter);
    Task<List<CustomerReportRow>> GetCustomerReportAsync(ReportFilter filter);
}
