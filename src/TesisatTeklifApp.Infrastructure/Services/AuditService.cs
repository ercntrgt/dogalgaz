using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string entityType, int entityId, string action, string? userName, string? description = null)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserName = userName,
            Description = description
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<ActivityLog>> GetForAsync(string entityType, int entityId) =>
        _db.ActivityLogs.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

    public Task<List<ActivityLog>> GetRecentAsync(int count = 100) =>
        _db.ActivityLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedDate)
            .Take(count)
            .ToListAsync();
}
