namespace TesisatTeklifApp.Domain.Enums;

/// <summary>Bir kalemin stok durumu.</summary>
public enum StockStatus
{
    NotTracked = 0,
    Sufficient = 1,
    Critical = 2,
    Insufficient = 3
}

/// <summary>Teklif / sipariş yaşam döngüsü durumu.</summary>
public enum OfferStatus
{
    Draft = 0,
    SentToCustomer = 1,
    Approved = 2,
    ConvertedToOrder = 3,
    WaitingSupply = 4,
    Completed = 5,
    Cancelled = 6
}

/// <summary>Stoktan düşme zamanı ayarı.</summary>
public enum StockDeductionMode
{
    DeductOnApproval = 0,
    DeductOnCompletion = 1,
    Manual = 2
}

/// <summary>Stok hareketi türü.</summary>
public enum MovementType
{
    Out = 0,      // Stoktan düşüş (sipariş)
    In = 1,       // Stok girişi
    Reserve = 2,  // Rezervasyon
    Restore = 3   // İptal iadesi
}

/// <summary>Ödeme türü. (Other yalnızca eski kayıtların görüntülenmesi için tutulur.)</summary>
public enum PaymentType
{
    Cash = 0,
    CreditCard = 1,
    Other = 2,
    BankTransfer = 3,
    Check = 4
}

/// <summary>Teklifte seçilebilen ödeme yöntemleri (Other listede yok).</summary>
public static class PaymentTypes
{
    public static readonly PaymentType[] Selectable =
        { PaymentType.Cash, PaymentType.CreditCard, PaymentType.BankTransfer, PaymentType.Check };
}

/// <summary>Satınalma siparişi durumu.</summary>
public enum PurchaseStatus
{
    Draft = 0,              // Taslak
    Ordered = 1,           // Sipariş verildi
    PartiallyReceived = 2, // Kısmen teslim alındı
    Received = 3,          // Teslim alındı
    Cancelled = 4          // İptal
}

/// <summary>Servis kaydı durumu.</summary>
public enum ServiceStatus
{
    Yeni = 0,
    RandevuVerildi = 1,
    Tamamlandi = 2,
    Iptal = 3
}

/// <summary>Servis nedeni (çoklu seçim — Servis Formu'ndaki kutucuklar).</summary>
[Flags]
public enum ServiceReason
{
    None = 0,
    Servis = 1,
    Bakim = 2,
    ArizaOnarimi = 4,
    Montaj = 8,
    DevreyeAlma = 16,
    YerindeOnarim = 32,
    PetekTemizligi = 64,
    AtolyeyeAlindi = 128
}
