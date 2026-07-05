using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Application.DTOs;

/// <summary>Servis kayıtları listesi filtresi.</summary>
public class ServiceRecordFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Keyword { get; set; }        // servis no / müşteri / cihaz
    public ServiceStatus? Status { get; set; }
    public ServiceReason? Reason { get; set; }
    public int? CustomerId { get; set; }
}
