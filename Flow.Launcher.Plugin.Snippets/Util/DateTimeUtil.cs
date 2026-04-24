using System;

namespace Flow.Launcher.Plugin.Snippets.Util;

public class DateTimeUtil
{
    public static DateTime TrimMilliseconds(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
    }

    public static string FormatDateTime(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }
}