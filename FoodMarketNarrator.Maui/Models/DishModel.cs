using SQLite;

namespace food_market_narrator.Models;

public class DishModel
{
    public int DishId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int? ImageId { get; set; }
    public string? ImageFileName { get; set; }
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Trả về tên file ảnh không có extension cho MAUI Resources.
    /// Ví dụ: "chilli_bbq.PNG" -> "chilli_bbq"
    /// </summary>
    public string? ImageResourceName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImageFileName))
                return null;

            // Loại bỏ extension (PNG, jpg, webp, etc.)
            var name = Path.GetFileNameWithoutExtension(ImageFileName);
            System.Diagnostics.Debug.WriteLine($"[DishModel] ImageFileName: {ImageFileName} -> ImageResourceName: {name}");
            return name;
        }
    }
}
