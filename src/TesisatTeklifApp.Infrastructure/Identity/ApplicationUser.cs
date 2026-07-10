using Microsoft.AspNetCore.Identity;

namespace TesisatTeklifApp.Infrastructure.Identity;

/// <summary>Uygulama kullanıcısı. Ad-soyad ile genişletilmiş Identity kullanıcısı.</summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>İmza-kaşe görseli (base64 PNG). Teklif PDF'inde "Firma Yetkilisi İmza" alanına basılır.</summary>
    public string? SignatureStamp { get; set; }
}

/// <summary>Sabit rol adları.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string SalesPerson = "SalesPerson";
    public const string Viewer = "Viewer";

    public static readonly string[] All = { Admin, SalesPerson, Viewer };

    public static string DisplayName(string role) => role switch
    {
        Admin => "Yönetici",
        SalesPerson => "Satış Personeli",
        Viewer => "Görüntüleyici",
        _ => role
    };
}
