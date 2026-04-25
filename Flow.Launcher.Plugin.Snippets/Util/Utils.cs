using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class Utils
{
    public static int BoolToInt(bool? value)
    {
        if (value == null) return 0;
        return (bool)value ? 1 : 0;
    }

    public static bool IntToBool(int? value)
    {
        if (value == null) return false;
        return value != 0;
    }

    public static BitmapImage LoadPluginIcon(PluginInitContext context)
    {
        var ico = LoadImage(context, Snippets.PluginIcoPath);
        if (ico != null)
            return ico;

        var png = LoadImage(context, Snippets.PluginPngIconPath);
        if (png != null)
            return png;
        
        return null;
    }

    public static BitmapImage LoadImage(PluginInitContext context, string path)
    {
        var imagePath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, path);
        return File.Exists(imagePath) ? new BitmapImage(new Uri(imagePath, UriKind.Absolute)) : null;
    }
}