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
}