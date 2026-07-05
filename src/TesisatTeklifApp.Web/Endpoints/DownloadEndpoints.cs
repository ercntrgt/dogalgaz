using Microsoft.AspNetCore.Authorization;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;

namespace TesisatTeklifApp.Web.Endpoints;

/// <summary>PDF ve Excel indirme uç noktaları (stream döndürür).</summary>
public static class DownloadEndpoints
{
    public static IEndpointRouteBuilder MapDownloadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/download").RequireAuthorization();

        // Müşteri PDF'i: kalem fiyatları YAZILMAZ, yalnızca genel toplam.
        group.MapGet("/offer/{id:int}/pdf", async (int id, IPdfExportService pdf) =>
        {
            var (fileName, content) = await pdf.GenerateOfferPdfAsync(id, includeLinePrices: false);
            return Results.File(content, "application/pdf", fileName);
        });

        // İç/ofis PDF'i: tam fiyatlı (yalnızca yönetici).
        group.MapGet("/offer/{id:int}/pdf-internal", [Authorize(Roles = "Admin")] async (int id, IPdfExportService pdf) =>
        {
            var (fileName, content) = await pdf.GenerateOfferPdfAsync(id, includeLinePrices: true);
            return Results.File(content, "application/pdf", "OFIS_" + fileName);
        });

        // Servis Formu PDF'i
        group.MapGet("/service/{id:int}/pdf", async (int id, IPdfExportService pdf) =>
        {
            var (fileName, content) = await pdf.GenerateServicePdfAsync(id);
            return Results.File(content, "application/pdf", fileName);
        });

        group.MapGet("/offers/excel", [Authorize(Roles = "Admin")] async (IExcelExportService excel) =>
        {
            var (fileName, content) = await excel.ExportOffersAsync(new OfferSearchFilter());
            return Results.File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        });

        // Ürün/stok Excel şablonu
        group.MapGet("/products/template", [Authorize(Roles = "Admin")] async (IProductService products) =>
        {
            var (fileName, content) = await products.ExportProductsExcelAsync();
            return Results.File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        });

        // Teklif/sipariş fotoğrafı (DB'den)
        group.MapGet("/photo/{id:int}", async (int id, IPhotoService photos) =>
        {
            var data = await photos.GetDataAsync(id);
            return data is null ? Results.NotFound() : Results.File(data.Value.Data, data.Value.ContentType);
        });

        // ===== Anonim: müşteri onay linkinden fiyatsız PDF (token ile) =====
        endpoints.MapGet("/teklif/{token}/pdf", async (string token, IOfferService offers, IPdfExportService pdf) =>
        {
            var offer = await offers.GetByPublicTokenAsync(token);
            if (offer is null) return Results.NotFound();
            var (fileName, content) = await pdf.GenerateOfferPdfAsync(offer.Id, includeLinePrices: false);
            return Results.File(content, "application/pdf", fileName);
        }).AllowAnonymous();

        return endpoints;
    }
}
