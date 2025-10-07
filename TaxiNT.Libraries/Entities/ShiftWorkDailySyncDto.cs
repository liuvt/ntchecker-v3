using TaxiNT.Libraries.Models;

namespace TaxiNT.Libraries.Entities
{
    public class ShiftWorkDailySyncDto
    {
        public ShiftWork ShiftWork { get; set; } = new();
        public List<TripDetail> TripDetails { get; set; } = new();
        public List<ContractDetail> ContractDetails { get; set; } = new();
    }
}
