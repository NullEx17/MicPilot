using System.Globalization;
using System.Windows.Data;

namespace MicPilot.App.Converters;

public sealed class LevelToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 ||
            values[0] is not double level ||
            values[1] is not double totalWidth)
        {
            return 0d;
        }

        var clamped = Math.Clamp(level, 0, 1);
        return totalWidth * clamped;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
