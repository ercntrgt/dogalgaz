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

/// <summary>Ödeme türü.</summary>
public enum PaymentType
{
    Cash = 0,
    CreditCard = 1,
    Other = 2
}
