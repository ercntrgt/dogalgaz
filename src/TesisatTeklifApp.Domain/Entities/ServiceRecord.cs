using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Servis kaydı (Servis Formu): cihaz arıza/bakım/montaj takibi.
/// Müşteriye bağlı olabilir; cihaz müşterinin daha önce aldığı üründen seçilebilir ya da elle girilir.
/// </summary>
public class ServiceRecord : BaseEntity
{
    public string ServiceNumber { get; set; } = string.Empty;   // SRV-2026-0001

    // Müşteri (opsiyonel; müşterisiz de servis açılabilir)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Tarihler
    public DateTime ApplicationDate { get; set; } = DateTime.Today;   // Başvuru
    public DateTime? AppointmentDate { get; set; }                    // Randevu
    public DateTime? RepairDate { get; set; }                         // Onarım

    // Cihaz
    public int? ServicedProductId { get; set; }                       // Müşterinin aldığı üründen
    public Product? ServicedProduct { get; set; }
    public string? DeviceBrand { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceType { get; set; }

    public string? ComplaintSubject { get; set; }                     // Şikayetin konusu
    public string? WorkDone { get; set; }                             // Yapılan işlem
    public string? SpecialNote { get; set; }                          // Özel not

    public ServiceReason ServiceReasons { get; set; }                 // [Flags] checkbox grid
    public decimal TotalAmount { get; set; }                          // Ödenecek genel toplam

    public string? TechnicianName { get; set; }
    public string? TechnicianSignature { get; set; }                  // base64
    public string? CustomerSignature { get; set; }                    // base64

    public ServiceStatus Status { get; set; } = ServiceStatus.Yeni;
    public string? CreatedBy { get; set; }
}
