using System.Globalization;

namespace LibrarySystem.UI.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
            return isAvailable ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#FFEBEE");
        return Colors.White;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
            return isAvailable ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");
        return Colors.Gray;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
            return isAvailable ? "✓ Rafta" : "✗ Ödünçte";
        return "";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFav) return isFav ? "❤️" : "🤍";
        return "🤍";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}