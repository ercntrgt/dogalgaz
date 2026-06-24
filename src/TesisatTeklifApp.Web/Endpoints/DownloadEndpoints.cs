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

        group.MapGet("/offer/{id:int}/pdf", [Authorize] async (int id, IPdfExportService pdf) =>
        {
            var (fileName, content) = await pdf.GenerateOfferPdfAsync(id);
            return Results.File(content, "application/pdf", fileName);
        });

        group.MapGet("/offers/excel", [Authorize] async (IExcelExportService excel) =>
        {
            var (fileName, content) = await excel.ExportOffersAsync(new OfferSearchFilter());
            return Results.File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        });

        return endpoints;
    }
}
