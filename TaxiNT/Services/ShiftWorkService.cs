using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        // kiểm tra dữ liệu đầu vào
        if (data?.ShiftWorks == null || data.ShiftWorks.Count == 0)
            return new BadRequestObjectResult("No ShiftWork data found.");

        //Tạo transaction để đảm bảo tính toàn vẹn dữ liệu. Tất cả sẽ Rollback nếu trong quá trình create/update/delete có lỗi xảy ra
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Đếm tổng số bản ghi
            int totalTrips = 0;
            int totalContracts = 0;

            // Xử lý từng nhóm ShiftWork
            foreach (var group in data.ShiftWorks)
            {
                // Lấy thông tin chung của 1 tài xế thông qua shiftWork: Khu vực + Tài xế + Ngày
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
                        x.createdAt.HasValue && //Kiểm tra trước khi null
                        x.createdAt.Value.Date == workDate);

                // Khởi tạo biến để giữ ShiftWork mục tiêu (mới hoặc cập nhật)
                ShiftWork targetShift;

                //Kiểm tra tồn tại thì cập nhật, không thì thêm mới
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

                    //Ghi lại dữ liệu để cập nhật
                    targetShift = existingShift;
                }
                else
                {
                    // --- Thêm mới ShiftWork ---
                    await _context.ShiftWorks.AddAsync(sw);

                    //Ghi lại liệu để thêm mới
                    targetShift = sw;
                }

                //Ghi vào SQL
                await _context.SaveChangesAsync();

                //Lấy Id của ShiftWork vừa thêm hoặc cập nhật để xữ lý Trip và Contract
                var shiftworkId = targetShift.Id;

                // --- Xóa dữ liệu cũ ---
                //Tìm contract và trip theo ID của shiftwork
                var oldTrips = _context.Trips.Where(t => t.shiftworkId == shiftworkId);
                var oldContracts = _context.Contracts.Where(c => c.shiftworkId == shiftworkId);
                //Xoá dữ liệu cũ
                _context.Trips.RemoveRange(oldTrips);
                _context.Contracts.RemoveRange(oldContracts);
                //Ghi vào SQL
                await _context.SaveChangesAsync();

                // --- Gán shiftworkId cho dữ liệu Trips mới ---
                foreach (var trip in group.Trips)
                {
                    trip.shiftworkId = shiftworkId;
                }
                // --- Gán shiftworkId cho dữ liệu Contracts mới ---
                foreach (var contract in group.Contracts)
                {
                    contract.shiftworkId = shiftworkId;
                }

                // --- Thêm dữ liệu mới ---
                await _context.Trips.AddRangeAsync(group.Trips);
                await _context.Contracts.AddRangeAsync(group.Contracts);

                //Ghi vào SQL lần cuối
                await _context.SaveChangesAsync();

                // Cập nhật tổng số bản ghi
                totalTrips += group.Trips.Count;
                totalContracts += group.Contracts.Count;
            }

            // Commit transaction nếu tất cả thành công sẽ được ghi vào database
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
            // Rollback transaction nếu có lỗi xảy ra, toàn bộ thay đổi sẽ bị hủy không tác động vào database
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during UpsertShiftWorkDailyAsync");
            return new ObjectResult("Internal server error") { StatusCode = 500 };
        }
    }
    #endregion
}
