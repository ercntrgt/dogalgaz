using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Constants;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Domain.Enums;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IStockControlService _stock;

    public ProductService(AppDbContext db, IStockControlService stock)
    {
        _db = db;
        _stock = stock;
    }

    public async Task<List<Product>> SearchAsync(ProductSearchFilter filter)
    {
        var q = _db.Products.AsQueryable();

        if (filter.OnlyActive == true)
            q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            // Ürün adları büyük harf saklanır — arama terimi de aynı dönüşümden geçer.
            var k = TextCasing.TrUpper(filter.Keyword.Trim())!;
            q = q.Where(p => p.Name.Contains(k)
                || (p.Brand != null && p.Brand.Contains(k))
                || (p.Model != null && p.Model.Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            q = q.Where(p => p.Category == filter.Category);

        if (filter.OnlyCriticalStock)
            q = q.Where(p => p.IsStockTracked && p.StockQuantity <= p.CriticalStockQuantity);

        return await q.AsNoTracking().OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Product>> GetByCategoryAsync(string category) =>
        _db.Products.AsNoTracking().Where(p => p.IsActive && p.Category == category)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

    public Task<List<Product>> GetActiveAsync() =>
        _db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return;
        product.IsDeleted = true;
        product.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task AdjustStockAsync(int productId, decimal newQuantity, string? note)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return;

        var previous = product.StockQuantity;
        product.StockQuantity = newQuantity;
        await _db.SaveChangesAsync();

        await _stock.CreateStockMovementAsync(productId, null, MovementType.In,
            newQuantity - previous, note ?? "Manuel stok düzeltme");
    }

    /// <summary>Verilen sıraya göre SortOrder'ı 0,1,2... olarak yeniden numaralar.</summary>
    public async Task SaveOrderAsync(IList<int> orderedIds)
    {
        if (orderedIds is null || orderedIds.Count == 0) return;

        var products = await _db.Products.Where(p => orderedIds.Contains(p.Id)).ToListAsync();
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var p = products.FirstOrDefault(x => x.Id == orderedIds[i]);
            if (p is not null) p.SortOrder = i;
        }
        await _db.SaveChangesAsync();
    }

    private static readonly string[] Headers =
    {
        "Id", "Ürün Adı", "Kategori", "Marka", "Model", "Birim",
        "Alış Fiyatı", "Satış Fiyatı", "KDV %", "Stok",
        "Kritik Stok", "Min Stok", "Stok Takibi (Evet/Hayır)", "Aktif (Evet/Hayır)"
    };

    public async Task<(string FileName, byte[] Content)> ExportProductsExcelAsync()
    {
        var products = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Urunler");
        for (var c = 0; c < Headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = Headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3a6b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var r = 2;
        foreach (var p in products)
        {
            ws.Cell(r, 1).Value = p.Id;
            ws.Cell(r, 2).Value = p.Name;
            ws.Cell(r, 3).Value = p.Category;
            ws.Cell(r, 4).Value = p.Brand ?? "";
            ws.Cell(r, 5).Value = p.Model ?? "";
            ws.Cell(r, 6).Value = p.Unit;
            ws.Cell(r, 7).Value = p.PurchasePrice;
            ws.Cell(r, 8).Value = p.SalePrice;
            ws.Cell(r, 9).Value = p.VatRate;
            ws.Cell(r, 10).Value = p.StockQuantity;
            ws.Cell(r, 11).Value = p.CriticalStockQuantity;
            ws.Cell(r, 12).Value = p.MinimumStockQuantity;
            ws.Cell(r, 13).Value = p.IsStockTracked ? "Evet" : "Hayır";
            ws.Cell(r, 14).Value = p.IsActive ? "Evet" : "Hayır";
            r++;
        }
        ws.Columns().AdjustToContents();

        // Yeni ürün için boş satıra ipucu (Id boş bırakılırsa yeni ürün eklenir).
        ws.Cell(r + 1, 2).Value = "(Yeni ürün eklemek için Id'yi boş bırakıp satır doldurun)";
        ws.Cell(r + 1, 2).Style.Font.Italic = true;
        ws.Cell(r + 1, 2).Style.Font.FontColor = XLColor.Gray;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ($"Urun_Stok_Sablonu_{DateTime.Now:yyyyMMdd}.xlsx", ms.ToArray());
    }

    public async Task<BulkImportResult> ImportProductsExcelAsync(byte[] data, string? user)
    {
        var result = new BulkImportResult();
        XLWorkbook wb;
        try { wb = new XLWorkbook(new MemoryStream(data)); }
        catch { result.Errors.Add("Dosya okunamadı. Geçerli bir .xlsx yükleyin."); return result; }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null || ws.LastRowUsed() is null)
            {
                result.Errors.Add("Sayfa boş.");
                return result;
            }

            // Başlık eşleştirme (sütun sırası esnek olsun)
            var map = new Dictionary<string, int>();
            foreach (var cell in ws.Row(1).CellsUsed())
                map[cell.GetString().Trim().ToLowerInvariant()] = cell.Address.ColumnNumber;

            int Col(params string[] names)
            {
                foreach (var n in names)
                    if (map.TryGetValue(n.ToLowerInvariant(), out var c)) return c;
                return -1;
            }

            int cId = Col("id");
            int cName = Col("ürün adı", "urun adi", "ad", "name");
            int cCat = Col("kategori", "category");
            int cBrand = Col("marka", "brand");
            int cModel = Col("model");
            int cUnit = Col("birim", "unit");
            int cBuy = Col("alış fiyatı", "alis fiyati", "purchaseprice");
            int cSale = Col("satış fiyatı", "satis fiyati", "saleprice", "fiyat");
            int cVat = Col("kdv %", "kdv", "vatrate");
            int cStock = Col("stok", "stockquantity");
            int cCrit = Col("kritik stok", "kritik", "criticalstockquantity");
            int cMin = Col("min stok", "minimum", "minimumstockquantity");
            int cTracked = Col("stok takibi (evet/hayır)", "stok takibi", "stok takibi (evet/hayir)");
            int cActive = Col("aktif (evet/hayır)", "aktif", "aktif (evet/hayir)");

            var lastRow = ws.LastRowUsed()!.RowNumber();
            for (var row = 2; row <= lastRow; row++)
            {
                var r = ws.Row(row);
                if (r.IsEmpty()) continue;

                var name = cName > 0 ? r.Cell(cName).GetString().Trim() : "";
                var idText = cId > 0 ? r.Cell(cId).GetString().Trim() : "";
                var hasId = int.TryParse(idText, out var id) && id > 0;

                // İpucu/boş satırları atla
                if (!hasId && string.IsNullOrWhiteSpace(name)) continue;
                if (!hasId && name.StartsWith("(")) continue;

                try
                {
                    Product? p;
                    if (hasId)
                    {
                        p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
                        if (p is null) { result.NotFoundCount++; result.Notes.Add($"Satır {row}: Id {id} bulunamadı."); continue; }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        p = new Product { Name = name, Category = cCat > 0 ? r.Cell(cCat).GetString().Trim() : "Diğer" };
                        _db.Products.Add(p);
                        result.Notes.Add($"Satır {row}: yeni ürün '{name}' eklendi.");
                    }

                    if (cName > 0 && !string.IsNullOrWhiteSpace(name)) p.Name = name;
                    if (cCat > 0) { var v = r.Cell(cCat).GetString().Trim(); if (!string.IsNullOrEmpty(v)) p.Category = v; }
                    if (cBrand > 0) p.Brand = r.Cell(cBrand).GetString().Trim();
                    if (cModel > 0) p.Model = r.Cell(cModel).GetString().Trim();
                    if (cUnit > 0) { var v = r.Cell(cUnit).GetString().Trim(); if (!string.IsNullOrEmpty(v)) p.Unit = v; }
                    if (cBuy > 0 && r.Cell(cBuy).TryGetValue<decimal>(out var buy)) p.PurchasePrice = buy;
                    if (cSale > 0 && r.Cell(cSale).TryGetValue<decimal>(out var sale)) p.SalePrice = sale;
                    if (cVat > 0 && r.Cell(cVat).TryGetValue<decimal>(out var vat)) p.VatRate = vat;
                    if (cCrit > 0 && r.Cell(cCrit).TryGetValue<decimal>(out var crit)) p.CriticalStockQuantity = crit;
                    if (cMin > 0 && r.Cell(cMin).TryGetValue<decimal>(out var min)) p.MinimumStockQuantity = min;
                    if (cTracked > 0) p.IsStockTracked = ParseBool(r.Cell(cTracked).GetString(), p.IsStockTracked);
                    if (cActive > 0) p.IsActive = ParseBool(r.Cell(cActive).GetString(), p.IsActive);

                    // Stok değişimi → hareket kaydı
                    if (cStock > 0 && r.Cell(cStock).TryGetValue<decimal>(out var newStock))
                    {
                        var previous = p.StockQuantity;
                        if (newStock != previous)
                        {
                            p.StockQuantity = newStock;
                            // Id'si henüz yoksa kaydet ki ProductId hareket için hazır olsun
                            await _db.SaveChangesAsync();
                            _db.StockMovements.Add(new StockMovement
                            {
                                ProductId = p.Id,
                                MovementType = newStock >= previous ? MovementType.In : MovementType.Out,
                                Quantity = Math.Abs(newStock - previous),
                                PreviousStock = previous,
                                NewStock = newStock,
                                Description = $"Toplu Excel güncelleme ({user})"
                            });
                            result.StockChangedCount++;
                        }
                    }

                    result.UpdatedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {row}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
        }

        return result;
    }

    private static bool ParseBool(string? s, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        s = s.Trim().ToLowerInvariant();
        if (s is "evet" or "true" or "1" or "aktif" or "var" or "e") return true;
        if (s is "hayır" or "hayir" or "false" or "0" or "pasif" or "yok" or "h") return false;
        return fallback;
    }
}
