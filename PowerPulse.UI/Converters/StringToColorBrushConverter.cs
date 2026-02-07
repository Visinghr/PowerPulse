using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PowerPulse.UI.Converters;

/// <summary>
/// Converts a hex color string (e.g., "#4CAF50") to a SolidColorBrush.
/// Used for dynamic health color binding in XAML.
/// </summary>
public class StringToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Fall through to default
            }
        }
        return new SolidColorBrush(System.Windows.Media.Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
