using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MicPilot.App.Converters;

public sealed class LevelBarConverter : IValueConverter
{
    private static readonly SolidColorBrush Off = Freeze(System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x2A));
    private static readonly SolidColorBrush On = Freeze(System.Windows.Media.Color.FromRgb(0x32, 0xDC, 0x5F));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var level = value is double d ? d : 0;
        if (!int.TryParse(parameter?.ToString(), out var index))
        {
            return Off;
        }

        return level >= (index + 0.35) / 12.0 ? On : Off;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Freeze(System.Windows.Media.Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
