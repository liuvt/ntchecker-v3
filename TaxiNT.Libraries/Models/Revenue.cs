
namespace TaxiNT.Libraries.Models;

public class ModelRevenueDetail
{
    public string Id { get; set; } = string.Empty;
    public string numberCar { get; set; } = string.Empty;
    public string userId { get; set; } = string.Empty;
    public string revenueByMonth { get; set; } = string.Empty;
    public string revenueByDate { get; set; } = string.Empty;
    public string qrContext { get; set; } = string.Empty;
    public string qrUrl { get; set; } = string.Empty;
    public decimal? discountOther { get; set; }
    public decimal? arrearsOther { get; set; }
    public decimal? totalPrice { get; set; }
    public decimal? walletGSM { get; set; }
    public decimal? discountGSM { get; set; }
    public decimal? discountNT { get; set; }
    public string bank_Id { get; set; } = string.Empty;
    public string typeCar { get; set; } = string.Empty;
    public DateTime? createdAt { get; set; }
}
