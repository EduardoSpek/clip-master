using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipMaster.Presentation.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.Visibility v && v == System.Windows.Visibility.Visible;
    }
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "ATIVO" : "INATIVO";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? new SolidColorBrush(Color.FromRgb(0, 212, 170)) : new SolidColorBrush(Color.FromRgb(255, 71, 87));
        return new SolidColorBrush(Color.FromRgb(255, 71, 87));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RecordingStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.FromRgb(233, 69, 96));
        return new SolidColorBrush(Color.FromRgb(100, 100, 130));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
