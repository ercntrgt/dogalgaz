using Microsoft.EntityFrameworkCore;
using TesisatTeklifApp.Application.DTOs;
using TesisatTeklifApp.Application.Interfaces;
using TesisatTeklifApp.Domain.Entities;
using TesisatTeklifApp.Infrastructure.Data;

namespace TesisatTeklifApp.Infrastructure.Services;

public class ServiceRecordService : IServiceRecordService
{
    private readonly AppDbContext _db;
    private readonly INumberGeneratorService _numbers;

    public ServiceRecordService(AppDbContext db, INumberGeneratorService numbers)
    {
        _db = db;
        _numbers = numbers;
    }

    public async Task<List<ServiceRecord>> SearchAsync(ServiceRecordFilter filter)
    {
        var q = _db.ServiceRecords.Include(s => s.Customer).AsQueryable();

        if (filter.FromDate.HasValue) q = q.Where(s => s.ApplicationDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) q = q.Where(s => s.ApplicationDate <= filter.ToDate.Value.Date);
        if (filter.Status.HasValue) q = q.Where(s => s.Status == filter.Status);
        if (filter.CustomerId.HasValue) q = q.Where(s => s.CustomerId == filter.CustomerId);
        if (filter.Reason.HasValue) q = q.Where(s => (s.ServiceReasons & filter.Reason.Value) != 0);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            q = q.Where(s => s.ServiceNumber.Contains(k)
                || (s.CustomerName != null && s.CustomerName.Contains(k))
                || (s.Customer != null && (s.Customer.FirstName + " " + s.Customer.LastName).Contains(k))
                || (s.DeviceBrand != null && s.DeviceBrand.Contains(k))
                || (s.DeviceModel != null && s.DeviceModel.Contains(k)));
        }

        return await q.OrderByDescending(s => s.ApplicationDate).ThenByDescending(s => s.Id).ToListAsync();
    }

    public Task<ServiceRecord?> GetByIdAsync(int id) =>
        _db.ServiceRecords
            .Include(s => s.Customer)
            .Include(s => s.ServicedProduct)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<ServiceRecord>> GetByCustomerAsync(int customerId) =>
        _db.ServiceRecords.Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.ApplicationDate).ToListAsync();

    public async Task SaveAsync(ServiceRecord record)
    {
        if (record.Id == 0)
        {
            if (string.IsNullOrEmpty(record.ServiceNumber))
                record.ServiceNumber = await _numbers.GenerateServiceNumberAsync();
            _db.ServiceRecords.Add(record);
        }
        else
        {
            _db.ServiceRecords.Update(record);
        }
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var s = await _db.ServiceRecords.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return;
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}
