
namespace TaxiNT.Libraries.Models;

public class ModelContractDetail
{
    public string ctId { get; set; } = string.Empty;
    public string numberCar { get; set; } = string.Empty;
    public string ctKey { get; set; } = string.Empty;
    public decimal? ctAmount { get; set; }
    public string ctDefaultDistance { get; set; } = string.Empty; // Khoản các mặc định
    public string ctOverDistance { get; set; } = string.Empty;  // Khoản cách vượt
    public decimal? ctSurcharge { get; set; }// Phụ phí
    public decimal? ctPromotion { get; set; } // Khuyến mãi
    public decimal? totalPrice { get; set; } // Thành tiền
    public string userId { get; set; } = string.Empty; // Key
    public DateTime? createdAt { get; set; }
}
