using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiNT.Controllers;
using TaxiNT.Data;
using TaxiNT.Libraries.Entities;
using TaxiNT.Libraries.Models;
using TaxiNT.Services.Interfaces;

namespace TaxiNT.Services;
//WithSQL
public class ShiftWorkService : IShiftWorkService
{
    #region SQL Controctor
    private readonly taxiNTDBContext _context;
    private readonly ILogger<ShiftWorkService> _logger;

    public ShiftWorkService(taxiNTDBContext context, ILogger<ShiftWorkService> logger)
    {
        this._context = context;
        this._logger = logger;
    }
    #endregion

    #region CURD
    // Upsert dữ liệu hằng ngày cho 3 bảng
    public async Task<object> UpsertShiftWorkDailyAsync(ShiftWorkDailySyncDto data)
    {
        if (data?.ShiftWorks == null || data.ShiftWorks.Count == 0)
            return new BadRequestObjectResult("No ShiftWork data found.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            int totalTrips = 0; // Đếm tổng số Trip được thêm
            int totalContracts = 0; // Đếm tổng số Contract được thêm

            foreach (var group in data.ShiftWorks)
            {
                var sw = group.ShiftWork; 
                if (sw.createdAt == null)
                    throw new Exception("ShiftWork.createdAt is required to determine WorkDate.");

                var workDate = sw.createdAt.Value.Date;
                var area = sw.Area;
                var userId = sw.userId;

                // === 1️⃣ Tìm ShiftWork hiện có theo Area + User + Ngày ===
                var existingShift = await _context.ShiftWorks
                    .FirstOrDefaultAsync(x =>
                        x.Area == area &&
                        x.userId == userId &&
                        x.createdAt.HasValue &&
                        x.createdAt.Value.Date == workDate);

                ShiftWork targetShift;

                if (existingShift != null)
                {
                    // Update dữ liệu
                    _context.Entry(existingShift).CurrentValues.SetValues(sw);
                    targetShift = existingShift;
                }
                else
                {
                    sw.createdAt ??= DateTime.UtcNow;
                    await _context.ShiftWorks.AddAsync(sw);
                    targetShift = sw;
                }

                await _context.SaveChangesAsync();
                var shiftworkId = targetShift.Id;

                // === 2️⃣ Xóa dữ liệu cũ của ShiftWork ===
                var oldTrips = _context.Trips.Where(t => t.shiftworkId == shiftworkId);
                var oldContracts = _context.Contracts.Where(c => c.shiftworkId == shiftworkId);

                _context.Trips.RemoveRange(oldTrips);
                _context.Contracts.RemoveRange(oldContracts);
                await _context.SaveChangesAsync();

                // === 3️⃣ Gán shiftworkId mới cho dữ liệu chi tiết ===
                foreach (var trip in group.Trips)
                {
                    trip.shiftworkId = shiftworkId;
                    trip.createdAt ??= DateTime.UtcNow;
                }

                foreach (var contract in group.Contracts)
                {
                    contract.shiftworkId = shiftworkId;
                    contract.createdAt ??= DateTime.UtcNow;
                }

                // === 4️⃣ Thêm dữ liệu mới ===
                await _context.Trips.AddRangeAsync(group.Trips);
                await _context.Contracts.AddRangeAsync(group.Contracts);
                await _context.SaveChangesAsync();

                totalTrips += group.Trips.Count;
                totalContracts += group.Contracts.Count;
            }

            await transaction.CommitAsync();

            return new OkObjectResult(new
            {
                message = "Upsert completed successfully",
                totalShiftWorks = data.ShiftWorks.Count,
                totalTrips,
                totalContracts
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during UpsertShiftWorkDailyAsync");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }
    #endregion
}
