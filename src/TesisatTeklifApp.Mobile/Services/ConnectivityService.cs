using Microsoft.Maui.Networking;

namespace TesisatTeklifApp.Mobile.Services;

/// <summary>İnternet bağlantısı durumunu izler; değişince olay yayar.</summary>
public class ConnectivityService
{
    public bool IsOnline
    {
        get
        {
            var a = Connectivity.Current.NetworkAccess;
            return a is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;
        }
    }

    public event Action? StateChanged;

    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += (_, _) => StateChanged?.Invoke();
    }
}
