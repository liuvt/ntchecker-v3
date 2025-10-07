
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiNT.Libraries.Models;

public class Contract
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public List<ContractDetail> ContractDetails { get; set; } = new();

    [NotMapped]
    public int RecordTotal => ContractDetails.Count; // Không lưu vào SQL
}


public class ContractDetail
{
    [Key]
    public string ctId { get; set; } = Guid.NewGuid().ToString();

    public string numberCar { get; set; } = string.Empty;
    public string ctKey { get; set; } = string.Empty;
    public decimal ctAmout { get; set; }

    public string ctDefaultDistance { get; set; } = string.Empty;
    public string ctOverDistance { get; set; } = string.Empty;
    public decimal ctSurcharge { get; set; }
    public decimal ctPromotion { get; set; } 
    public decimal totalPrice { get; set; }

    public string userId { get; set; } = string.Empty;
    public DateTime createdAt { get; set; }

    // Trỏ tới khóa ngoại Revenue
    [ForeignKey(nameof(ShiftWork))]
    public string? revenueId { get; set; }
    public ShiftWork? ShiftWork { get; set; }
}
