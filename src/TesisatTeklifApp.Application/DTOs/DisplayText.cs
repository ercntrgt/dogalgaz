using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Teklif durumu için Türkçe etiketler.</summary>
public static class OfferStatusText
{
    public static string Label(OfferStatus status) => status switch
    {
        OfferStatus.Draft => "Taslak",
        OfferStatus.SentToCustomer => "Müşteriye Gönderildi",
        OfferStatus.Approved => "Onaylandı",
        OfferStatus.ConvertedToOrder => "Siparişe Dönüştürüldü",
        OfferStatus.WaitingSupply => "Bekleyen Tedarik",
        OfferStatus.Completed => "Tamamlandı",
        OfferStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };

    public static readonly IReadOnlyList<OfferStatus> All = Enum.GetValues<OfferStatus>();
}

/// <summary>Ödeme türü etiketleri.</summary>
public static class PaymentTypeText
{
    public static string Label(PaymentType t) => t switch
    {
        PaymentType.Cash => "Nakit",
        PaymentType.CreditCard => "Kredi Kartı",
        PaymentType.BankTransfer => "Havale / EFT",
        PaymentType.Check => "Çek / Senet",
        _ => "Diğer"
    };
}
