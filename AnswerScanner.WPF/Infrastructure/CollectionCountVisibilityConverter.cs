using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AnswerScanner.WPF.Infrastructure;

public class CollectionCountVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int count => count > 0 ? Visibility.Visible : Visibility.Collapsed,
            ICollection collection => collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}