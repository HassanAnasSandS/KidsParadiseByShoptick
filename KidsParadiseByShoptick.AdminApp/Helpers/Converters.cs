using System.Globalization;
using KidsParadiseByShoptick.AdminApp.Config;

namespace KidsParadiseByShoptick.AdminApp.Helpers;

public class ImageUrlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var url = value as string;
        if (string.IsNullOrWhiteSpace(url)) return null;
        return AppSettings.ResolveImageUrl(url);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && !string.IsNullOrWhiteSpace(value.ToString());

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PositiveAmountConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return false;
        if (value is decimal d) return d > 0;
        if (value is double dbl) return dbl > 0;
        if (value is int i) return i > 0;
        return decimal.TryParse(value.ToString(), NumberStyles.Number, culture, out var parsed) && parsed > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public static class FormatHelpers
{
    public static string Price(decimal amount) => $"Rs. {amount:N0}";
}
