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
            int totalTrips = 0;
            int totalContracts = 0;

            foreach (var group in data.ShiftWorks)
            {
                var sw = group.ShiftWork;
                if (sw.createdAt == null)
                    throw new Exception("ShiftWork.createdAt is required to determine WorkDate.");

                var workDate = sw.createdAt.Value.Date;
                var area = sw.Area;
                var userId = sw.userId;

                // Tìm ShiftWork hiện có theo Area + User + Ngày
                var existingShift = await _context.ShiftWorks
                    .FirstOrDefaultAsync(x =>
                        x.Area == area &&
                        x.userId == userId &&
                        x.createdAt.HasValue &&
                        x.createdAt.Value.Date == workDate);

                ShiftWork targetShift;

                if (existingShift != null)
                {
                    // --- Update từng property để EF nhận thay đổi ---
                    existingShift.numberCar = sw.numberCar;
                    existingShift.userId = sw.userId;
                    existingShift.revenueByMonth = sw.revenueByMonth;
                    existingShift.revenueByDate = sw.revenueByDate;
                    existingShift.qrContext = sw.qrContext;
                    existingShift.qrUrl = sw.qrUrl;
                    existingShift.discountOther = sw.discountOther;
                    existingShift.arrearsOther = sw.arrearsOther;
                    existingShift.totalPrice = sw.totalPrice;
                    existingShift.walletGSM = sw.walletGSM;
                    existingShift.discountGSM = sw.discountGSM;
                    existingShift.discountNT = sw.discountNT;
                    existingShift.bank_Id = sw.bank_Id;
                    existingShift.createdAt = sw.createdAt;
                    existingShift.typeCar = sw.typeCar;
                    existingShift.Area = sw.Area;
                    existingShift.Rank = sw.Rank;
                    existingShift.SauMucAnChia = sw.SauMucAnChia;

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

                // --- Xóa dữ liệu cũ ---
                var oldTrips = _context.Trips.Where(t => t.shiftworkId == shiftworkId);
                var oldContracts = _context.Contracts.Where(c => c.shiftworkId == shiftworkId);
                _context.Trips.RemoveRange(oldTrips);
                _context.Contracts.RemoveRange(oldContracts);
                await _context.SaveChangesAsync();

                // --- Gán shiftworkId cho dữ liệu mới ---
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

                // --- Thêm dữ liệu mới ---
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
