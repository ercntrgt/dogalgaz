using Microsoft.Maui.Storage;

namespace TesisatTeklifApp.Mobile.Services;

/// <summary>
/// Cihaz başına bir kez (internetle) giriş. Giriş bilgisi cihazda saklanır; tekrar sorulmaz.
/// Sunucu senkron API'si gelince doğrulama oraya bağlanacak; şimdilik demo hesaplar.
/// </summary>
public class LoginService
{
    private readonly ConnectivityService _conn;
    public LoginService(ConnectivityService conn) => _conn = conn;

    public bool IsLoggedIn => !string.IsNullOrEmpty(Preferences.Get("user", ""));
    public string CurrentUser => Preferences.Get("user", "");
    public string CurrentRole => Preferences.Get("role", "");

    /// <summary>
    /// Bu satışçı siparişe dönüştürmek için yönetici onayı gerektiriyor mu?
    /// Varsayılan: hayır (satışçı doğrudan siparişe dönüştürebilir). Yönetici sunucudan
    /// bu satışçı için açabilir (senkronla cihaza gelir). Şimdilik cihazda tutulur.
    /// </summary>
    public bool RequiresApproval => Preferences.Get("requiresApproval", false);
    public void SetRequiresApproval(bool v) => Preferences.Set("requiresApproval", v);

    public (bool ok, string? error) TryLogin(string email, string password)
    {
        if (!_conn.IsOnline)
            return (false, "İlk giriş için internet bağlantısı gerekli. Bağlanıp tekrar deneyin.");

        var u = (email ?? "").Trim().ToLowerInvariant();
        string? role = null;
        if (u == "admin@ozdemir.local" && password == "Admin!123") role = "Admin";
        else if (u == "satis@ozdemir.local" && password == "Satis!123") role = "SalesPerson";
        else if (u == "gozlemci@ozdemir.local" && password == "Gozlem!123") role = "Viewer";

        if (role is null)
            return (false, "E-posta veya şifre hatalı.");

        Preferences.Set("user", email!.Trim());
        Preferences.Set("role", role);
        return (true, null);
    }

    public void Logout()
    {
        Preferences.Remove("user");
        Preferences.Remove("role");
    }
}
