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

    /// <summary>Verilen sıraya göre ürünlerin SortOrder değerini yeniden numaralar (0,1,2...).</summary>
    Task SaveOrderAsync(IList<int> orderedIds);

    /// <summary>Tüm ürünleri Excel şablonu olarak verir (düzenlenip geri yüklenir).</summary>
    Task<(string FileName, byte[] Content)> ExportProductsExcelAsync();

    /// <summary>Excel'den toplu fiyat/stok güncellemesi yapar (Id ile eşleştirir).</summary>
    Task<BulkImportResult> ImportProductsExcelAsync(byte[] data, string? user);
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
    Task<List<Offer>> GetByCustomerAsync(int customerId);   // ödeme planlarıyla, takip için
    Task<List<Offer>> GetWorkScheduleAsync(DateTime? from, DateTime? to);  // iş/montaj takvimi
    Task<Offer?> GetByIdAsync(int id);          // tüm satırlarıyla
    Task<Offer> CreateDraftAsync(int customerId, string? responsiblePerson);
    Task SaveAsync(Offer offer);                // ekle/güncelle + yeniden hesapla
    Task<Offer> CopyAsync(int offerId, string? createdBy = null);   // teklif kopyalama
    Task ChangeStatusAsync(int offerId, OfferStatus status);
    Task<Offer> ConvertToOrderAsync(int offerId);   // siparişe dönüştür (+ stok kontrol)
    Task CancelAsync(int offerId);              // iptal + stok iadesi
    Task SaveSignatureAsync(int offerId, string? signatureBase64);   // müşteri imzası
    Task<Offer?> GetByPublicTokenAsync(string token);                // müşteri onay linki (anonim)
    Task<bool> ApproveByTokenAsync(string token, string signatureBase64);   // linkten imza + onay
    Task DeleteAsync(int offerId);              // kalıcı (soft) silme — yönetici
    Task RequestDeleteAsync(int offerId, string user);   // silme talebi — satış
    Task RejectDeleteAsync(int offerId);        // silme talebini reddet — yönetici
    Task MarkDeliveredAsync(int offerId, string? userName);          // teslim edildi (tamamlandı)
}

public interface ISupplierService
{
    Task<List<Supplier>> SearchAsync(string? keyword);
    Task<Supplier?> GetByIdAsync(int id);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task SoftDeleteAsync(int id);
}

public interface IPurchaseService
{
    Task<List<PurchaseOrder>> SearchAsync(PurchaseStatus? status);
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<PurchaseOrder> CreateAsync(PurchaseOrder order);
    Task UpdateStatusAsync(int id, PurchaseStatus status);

    /// <summary>Tüm kalemleri teslim alır: ürün stoğunu artırır + stok hareketi oluşturur.</summary>
    Task ReceiveAllAsync(int purchaseOrderId);

    /// <summary>Kritik/eksik stoktaki ürünlerden satınalma önerisi üretir.</summary>
    Task<List<PurchaseSuggestionRow>> GetSuggestionsAsync();
}

public interface IPaymentService
{
    /// <summary>Vade takvimi: tüm ödeme satırlarını müşteri/sipariş bilgisiyle döndürür.</summary>
    Task<List<PaymentScheduleRow>> GetScheduleAsync(PaymentScheduleFilter filter);
    Task<PaymentScheduleSummary> GetSummaryAsync();
    Task MarkPaidAsync(int paymentPlanId, bool isPaid);
}

public interface IPhotoService
{
    Task<int> AddAsync(int offerId, string fileName, string contentType, byte[] data, string? user);
    Task<List<PhotoInfo>> GetForOfferAsync(int offerId);
    Task<(byte[] Data, string ContentType)?> GetDataAsync(int id);
    Task DeleteAsync(int id);
}

public interface IAuditService
{
    Task LogAsync(string entityType, int entityId, string action, string? userName, string? description = null);
    Task<List<ActivityLog>> GetForAsync(string entityType, int entityId);
    Task<List<ActivityLog>> GetRecentAsync(int count = 100);
}

public interface INumberGeneratorService
{
    Task<string> GenerateOfferNumberAsync();    // TSF-2026-0001
    Task<string> GenerateOrderNumberAsync();     // SPR-2026-0001
    Task<string> GenerateServiceNumberAsync();   // SRV-2026-0001
}

public interface IUstaService
{
    Task<List<Usta>> SearchAsync(string? keyword);
    Task<List<Usta>> GetActiveAsync();
    Task<Usta?> GetByIdAsync(int id);
    Task AddAsync(Usta usta);
    Task UpdateAsync(Usta usta);
    Task SoftDeleteAsync(int id);
    /// <summary>Verilen ad ile aktif usta bulur, yoksa oluşturur (tablet senkron / hızlı ekleme).</summary>
    Task<Usta> GetOrCreateByNameAsync(string name);
}

public interface IHakedisService
{
    /// <summary>Her usta için toplam hakediş − toplam ödeme = bakiye.</summary>
    Task<List<UstaBalanceRow>> GetUstaBalancesAsync();
    /// <summary>Haftalık hakediş tablosu (iş bazlı hakedişler, hafta gruplarıyla).</summary>
    Task<List<HakedisWeekRow>> GetWeeklyScheduleAsync(HakedisFilter filter);
    Task<HakedisSummary> GetSummaryAsync();
    Task AddPaymentAsync(int ustaId, decimal amount, DateTime paidDate, string? note, string? user);
    Task<List<UstaPayment>> GetPaymentsAsync(int ustaId);
    Task DeletePaymentAsync(int paymentId);
}

public interface IServiceRecordService
{
    Task<List<ServiceRecord>> SearchAsync(ServiceRecordFilter filter);
    Task<ServiceRecord?> GetByIdAsync(int id);
    Task<List<ServiceRecord>> GetByCustomerAsync(int customerId);
    Task SaveAsync(ServiceRecord record);       // ekle/güncelle (+ numara üret)
    Task SoftDeleteAsync(int id);
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
    /// <summary>
    /// Teklif/sipariş PDF'i üretir. <paramref name="includeLinePrices"/> false ise
    /// müşteri PDF'i: kalem/satır fiyatları yazılmaz, yalnızca genel toplam görünür.
    /// </summary>
    Task<(string FileName, byte[] Content)> GenerateOfferPdfAsync(int offerId, bool includeLinePrices = true);

    /// <summary>Servis Formu PDF'i üretir.</summary>
    Task<(string FileName, byte[] Content)> GenerateServicePdfAsync(int serviceId);
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
