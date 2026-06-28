using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Application.Services;
using TesisatTeklifApp.Mobile.Data;
using TesisatTeklifApp.Mobile.Services;

namespace TesisatTeklifApp.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// --- Yerel (offline) veritabanı: cihazın AppData klasöründe SQLite ---
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tesisat_local.db");
		builder.Services.AddDbContext<LocalDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

		// Saf hesaplama servisi (sunucu ile ortak)
		builder.Services.AddScoped<IOfferCalculationService, OfferCalculationService>();

		// Offline veri + senkron servisleri
		builder.Services.AddScoped<LocalDataService>();
		builder.Services.AddSingleton<ConnectivityService>();
		builder.Services.AddSingleton<LoginService>();
		builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
		builder.Services.AddScoped<SyncService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Yerel veritabanını oluştur + ilk açılışta örnek katalog (senkron gelene kadar).
		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
			db.Database.EnsureCreated();   // şema (hızlı). Katalog seed'i ilk veri erişiminde async yapılır.
		}

		return app;
	}
}
