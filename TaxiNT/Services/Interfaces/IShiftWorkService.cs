using TaxiNT.Libraries.Entities;

namespace TaxiNT.Services.Interfaces;
public interface IShiftWorkService
{
    Task<object> UpsertShiftWorkDailyAsync(ShiftWorkDailySyncDto data);
}
