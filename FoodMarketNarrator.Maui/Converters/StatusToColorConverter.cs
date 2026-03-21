using System.Globalization;

namespace food_market_narrator.Converters;

/// <summary>
/// Convert IsActive (bool) to status color.
/// IsActive = true (Mở cửa)  → Green (#2E7D32 / #C8E6C9)
/// IsActive = false (Đóng cửa) → Red (#C62828 / #FFCDD2)
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    // True = Open → Green
    public Color TrueColor { get; set; } = Color.FromArgb("#2E7D32");
    public Color TrueBackgroundColor { get; set; } = Color.FromArgb("#C8E6C9");

    // False = Closed → Red
    public Color FalseColor { get; set; } = Color.FromArgb("#C62828");
    public Color FalseBackgroundColor { get; set; } = Color.FromArgb("#FFCDD2");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;

        // Target type determines which color to return
        if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
        {
            if (parameter?.ToString() == "background")
                return isActive ? TrueBackgroundColor : FalseBackgroundColor;
            return isActive ? TrueColor : FalseColor;
        }

        // For string return values (kept for backwards compatibility)
        return isActive ? "#2E7D32" : "#C62828";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
