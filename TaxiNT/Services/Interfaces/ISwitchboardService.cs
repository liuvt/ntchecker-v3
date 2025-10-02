using TaxiNT.Libraries.Models;

namespace TaxiNT.Services.Interfaces
{
    public interface ISwitchboardService
    {
        Task<bool> AppendCallLogAsync(CallLog model);
    }
    
}
