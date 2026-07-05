using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Constants;

/// <summary>Servis nedeni bayrakları ↔ görünen etiketler (checkbox grid + PDF ortak).</summary>
public static class ServiceReasonLabels
{
    public static readonly IReadOnlyList<(ServiceReason Value, string Label)> All = new[]
    {
        (ServiceReason.Servis, "Servis"),
        (ServiceReason.Bakim, "Bakım"),
        (ServiceReason.ArizaOnarimi, "Arıza Onarımı"),
        (ServiceReason.Montaj, "Montaj"),
        (ServiceReason.DevreyeAlma, "Devreye Alma"),
        (ServiceReason.YerindeOnarim, "Yerinde Onarım"),
        (ServiceReason.PetekTemizligi, "Petek Temizliği"),
        (ServiceReason.AtolyeyeAlindi, "Atölyeye Alındı"),
    };

    public static string Text(ServiceReason reasons) =>
        reasons == ServiceReason.None
            ? "-"
            : string.Join(", ", All.Where(r => reasons.HasFlag(r.Value)).Select(r => r.Label));
}
