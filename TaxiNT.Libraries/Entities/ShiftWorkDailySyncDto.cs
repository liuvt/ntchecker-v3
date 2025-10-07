using TaxiNT.Libraries.Models;

namespace TaxiNT.Libraries.Entities
{
    public class ShiftWorkDailySyncDto
    {
        public List<ShiftWorkGroupDto> ShiftWorks { get; set; } = new();
    }

    public class ShiftWorkGroupDto
    {
        public ShiftWork ShiftWork { get; set; } = new();
        public List<TripDetail> Trips { get; set; } = new();
        public List<ContractDetail> Contracts { get; set; } = new();
    }
}
