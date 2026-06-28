using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Ödeme durumu (vade takvimi için hesaplanır).</summary>
public enum PaymentScheduleStatus
{
    Paid = 0,       // Tahsil edildi
    Upcoming = 1,   // Vadesi gelmemiş
    DueToday = 2,   // Bugün vadesi
    Overdue = 3,    // Gecikmiş
    NoDate = 4      // Vade tarihi girilmemiş
}

/// <summary>Ödeme takvimi satırı.</summary>
public class PaymentScheduleRow
{
    public int PaymentPlanId { get; set; }
    public int OfferId { get; set; }
    public string FormNumber { get; set; } = string.Empty;   // sipariş no varsa o, yoksa teklif no
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Description { get; set; }

    public PaymentScheduleStatus Status { get; set; }
    public int? DaysRemaining { get; set; }   // negatif = gecikme günü
}

/// <summary>Ödeme takvimi filtresi.</summary>
public class PaymentScheduleFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool OnlyUnpaid { get; set; }
    public bool OnlyOverdue { get; set; }
}

/// <summary>Ödeme takvimi özeti (kartlar için).</summary>
public class PaymentScheduleSummary
{
    public decimal TotalPending { get; set; }   // ödenmemiş toplam
    public decimal OverdueAmount { get; set; }  // gecikmiş toplam
    public decimal ThisMonthAmount { get; set; }// bu ay vadesi gelen (ödenmemiş)
    public decimal PaidAmount { get; set; }     // tahsil edilen toplam
}
