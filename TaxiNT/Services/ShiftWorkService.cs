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
        if (data == null || data.ShiftWork == null)
            return new BadRequestObjectResult("Invalid data payload.");

        // Lấy ngày và khu vực từ ShiftWork chính
        var workDate = data.ShiftWork.createdAt?.Date ?? DateTime.UtcNow.Date;
        var area = data.ShiftWork.Area;
        var userId = data.ShiftWork.userId;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // === 1️⃣ Upsert SHIFTWORK ===
            var existingShift = await _context.ShiftWorks
                .FirstOrDefaultAsync(x =>
                    x.Area == area &&
                    x.userId == userId &&
                    x.createdAt.HasValue &&
                    x.createdAt.Value.Date == workDate);

            ShiftWork targetShift;

            if (existingShift != null)
            {
                // Update nếu tồn tại
                existingShift.numberCar = data.ShiftWork.numberCar;
                existingShift.revenueByMonth = data.ShiftWork.revenueByMonth;
                existingShift.revenueByDate = data.ShiftWork.revenueByDate;
                existingShift.totalPrice = data.ShiftWork.totalPrice;
                existingShift.discountNT = data.ShiftWork.discountNT;
                existingShift.discountGSM = data.ShiftWork.discountGSM;
                existingShift.discountOther = data.ShiftWork.discountOther;
                existingShift.arrearsOther = data.ShiftWork.arrearsOther;
                existingShift.walletGSM = data.ShiftWork.walletGSM;
                existingShift.Rank = data.ShiftWork.Rank;
                existingShift.qrContext = data.ShiftWork.qrContext;
                existingShift.qrUrl = data.ShiftWork.qrUrl;
                existingShift.createdAt = data.ShiftWork.createdAt;

                _context.ShiftWorks.Update(existingShift);
                targetShift = existingShift;
            }
            else
            {
                // Tạo mới
                data.ShiftWork.createdAt ??= DateTime.UtcNow;
                await _context.ShiftWorks.AddAsync(data.ShiftWork);
                targetShift = data.ShiftWork;
            }

            await _context.SaveChangesAsync();

            // === 2️⃣ Gắn shiftworkId cho chi tiết ===
            var shiftworkId = targetShift.Id;

            foreach (var trip in data.TripDetails)
            {
                trip.shiftworkId = shiftworkId;
                trip.createdAt ??= DateTime.UtcNow;
            }

            foreach (var contract in data.ContractDetails)
            {
                contract.shiftworkId = shiftworkId;
                contract.createdAt ??= DateTime.UtcNow;
            }

            // === 3️⃣ Xóa dữ liệu cũ của user trong cùng ngày ===
            var oldTrips = _context.Trips.Where(x =>
                x.userId == userId &&
                x.createdAt.HasValue &&
                x.createdAt.Value.Date == workDate);

            _context.Trips.RemoveRange(oldTrips);

            var oldContracts = _context.Contracts.Where(x =>
                x.userId == userId &&
                x.createdAt.HasValue &&
                x.createdAt.Value.Date == workDate);

            _context.Contracts.RemoveRange(oldContracts);

            await _context.SaveChangesAsync();

            // === 4️⃣ Thêm mới ===
            await _context.Trips.AddRangeAsync(data.TripDetails);
            await _context.Contracts.AddRangeAsync(data.ContractDetails);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new OkObjectResult(new
            {
                message = "Upsert completed successfully",
                area,
                userId,
                workDate = workDate.ToString("yyyy-MM-dd"),
                totalTrips = data.TripDetails.Count,
                totalContracts = data.ContractDetails.Count
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during upsert ShiftWorkDaily for user {UserId}", userId);
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }
    #endregion
}
