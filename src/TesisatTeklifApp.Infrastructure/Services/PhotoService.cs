using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class PhotoService : IPhotoService
{
    private readonly AppDbContext _db;
    public PhotoService(AppDbContext db) => _db = db;

    public async Task<int> AddAsync(int offerId, string fileName, string contentType, byte[] data, string? user)
    {
        var photo = new OfferPhoto
        {
            OfferId = offerId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType,
            Data = data,
            UploadedBy = user
        };
        _db.OfferPhotos.Add(photo);
        await _db.SaveChangesAsync();
        return photo.Id;
    }

    // Byte verisini ÇEKMEDEN sadece meta verileri döndür (liste hafif kalsın).
    public Task<List<PhotoInfo>> GetForOfferAsync(int offerId) =>
        _db.OfferPhotos.AsNoTracking()
            .Where(p => p.OfferId == offerId)
            .OrderBy(p => p.CreatedDate)
            .Select(p => new PhotoInfo
            {
                Id = p.Id, FileName = p.FileName, ContentType = p.ContentType,
                CreatedDate = p.CreatedDate, UploadedBy = p.UploadedBy
            })
            .ToListAsync();

    public async Task<(byte[] Data, string ContentType)?> GetDataAsync(int id)
    {
        var p = await _db.OfferPhotos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : (p.Data, p.ContentType);
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.OfferPhotos.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return;
        _db.OfferPhotos.Remove(p);   // fotoğraf için hard delete uygun
        await _db.SaveChangesAsync();
    }
}
