namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Usta bakiye satırı (toplam hakediş − ödenen = bakiye).</summary>
public class UstaBalanceRow
{
    public int UstaId { get; set; }
    public string UstaName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? Phone { get; set; }
    public int JobCount { get; set; }
    public decimal TotalEarned { get; set; }    // tüm işlerden hakediş
    public decimal TotalPaid { get; set; }      // yapılan ödemeler
    public decimal Balance { get; set; }        // kalan alacak
    public decimal ThisWeekEarned { get; set; } // bu hafta biten işlerden
}

/// <summary>Haftalık hakediş tablosu satırı (iş bazlı).</summary>
public class HakedisWeekRow
{
    public int OfferId { get; set; }
    public string FormNumber { get; set; } = string.Empty;
    public int UstaId { get; set; }
    public string UstaName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }      // WorkEndDate ?? OfferDate
    public string WeekLabel { get; set; } = string.Empty;  // "23-29 Haz" gibi
    public decimal Earning { get; set; }
    public string StatusText { get; set; } = string.Empty;
}

/// <summary>Hakediş filtresi.</summary>
public class HakedisFilter
{
    public int? UstaId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>Hakediş özeti (kartlar için).</summary>
public class HakedisSummary
{
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal ThisWeekEarned { get; set; }
    public int ActiveUstaCount { get; set; }
}
