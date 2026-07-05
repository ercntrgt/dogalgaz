using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Application.Services;
using TesisatTeklifApp.Infrastructure.Data;
using TesisatTeklifApp.Infrastructure.Services;

namespace TesisatTeklifApp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure servislerini ve DbContext'i kaydeder.
    /// DatabaseProvider ayarı "SqlServer" ise SQL Server, aksi halde SQLite kullanılır.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config, string? logoPath = null)
    {
        // QuestPDF Community lisansı (küçük firmalar için ücretsiz).
        QuestPDF.Settings.License = LicenseType.Community;

        var provider = config["DatabaseProvider"] ?? "Sqlite";
        var conn = config.GetConnectionString("DefaultConnection")
                   ?? "Data Source=tesisat.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(conn);
            else if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                  || provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(conn);
            else
                options.UseSqlite(conn);
        });

        // Saf hesaplama servisi (Application katmanı).
        services.AddScoped<IOfferCalculationService, OfferCalculationService>();

        // Veri/iş servisleri.
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IUstaService, UstaService>();
        services.AddScoped<IHakedisService, HakedisService>();
        services.AddScoped<IServiceRecordService, ServiceRecordService>();
        services.AddScoped<INumberGeneratorService, NumberGeneratorService>();
        services.AddScoped<IStockControlService, StockControlService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IPdfExportService>(sp => new PdfExportService(
            sp.GetRequiredService<AppDbContext>(),
            sp.GetRequiredService<IStockControlService>(),
            logoPath));

        return services;
    }
}
