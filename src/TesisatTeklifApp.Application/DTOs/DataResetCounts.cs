namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Teslim öncesi temizlikte silinecek/silinen kayıt sayıları.</summary>
public class DataResetCounts
{
    public int Customers { get; set; }
    public int Offers { get; set; }
    public int PaymentPlans { get; set; }
    public int ServiceRecords { get; set; }
    public int Ustalar { get; set; }
    public int UstaPayments { get; set; }
    public int PurchaseOrders { get; set; }
    public int Suppliers { get; set; }
    public int StockMovements { get; set; }
    public int ActivityLogs { get; set; }

    /// <summary>Korunur — bilgi amaçlı gösterilir.</summary>
    public int Products { get; set; }

    public int Total => Customers + Offers + PaymentPlans + ServiceRecords + Ustalar
        + UstaPayments + PurchaseOrders + Suppliers + StockMovements + ActivityLogs;
}
