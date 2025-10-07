
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiNT.Libraries.Models;

public class Trip
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<TripDetail> TripDetails { get; set; } = new();

    [NotMapped]
    public int RecordTotal => TripDetails.Count; // Không lưu vào SQL
}

public class TripDetail
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NumberCar { get; set; } = string.Empty;
    public DateTime? tpTimeStart { get; set; }
    public DateTime? tpTimeEnd { get; set; }
    public double? tpDistance { get; set; }
    public decimal tpPrice { get; set; }
    public string tpPickUp { get; set; } = string.Empty;
    public string tpDropOut { get; set; } = string.Empty;
    public string tpType { get; set; } = string.Empty;
    public string userId { get; set; } = string.Empty;
    public DateTime? createdAt { get; set; }

    // Khóa ngoại tới bảng "Danh sách lên ca"
    [ForeignKey(nameof(ShiftWork))]
    public string? revenueId { get; set; }
    public ShiftWork? ShiftWork { get; set; }

}
