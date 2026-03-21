using System.Globalization;
using System.ComponentModel;

namespace food_market_narrator.Converters;

/// <summary>
/// Chuyển đổi null hoặc string rỗng thành Visibility.
/// - Default: string rỗng/null → Visible, có giá trị → Collapsed
/// - ConverterParameter=inverse: string rỗng/null → Collapsed, có giá trị → Visible
/// </summary>
public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(value as string);
        bool inverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) ?? false;

        if (inverse)
        {
            return isEmpty ? false : true; // Có giá trị → Visible
        }
        else
        {
            return isEmpty ? true : false; // Rỗng → Visible
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
