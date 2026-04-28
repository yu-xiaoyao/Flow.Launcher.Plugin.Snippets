using System;
using System.Globalization;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.Snippets.UI.Converters;

public class SnippetFavoritesToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // InnerLogger.Logger.Info($"IntToBoolConverter called for {value}, type: {value.GetType().Name}");
        if (value is int flag)
        {
            return flag != 0; // 0=false，其它=true
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked)
        {
            return isChecked ? 1 : 0;
        }

        return 0;
    }
}