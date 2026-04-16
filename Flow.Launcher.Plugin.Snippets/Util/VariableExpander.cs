using System;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.Snippets.Util;

/// <summary>
/// Utility class for expanding variables in snippet values.
/// Supports date and time variables with custom formatting.
/// </summary>
public static class VariableExpander
{
    /// <summary>
    /// Expands variables in the input text.
    /// Supported variables:
    /// - {{date}} or {{date:format}} - Current date
    /// - {{time}} or {{time:format}} - Current time
    /// - {{datetime}} or {{datetime:format}} - Current date and time
    /// - {{timestamp}} - Unix timestamp
    /// - {{year}}, {{month}}, {{day}}, {{hour}}, {{minute}}, {{second}} - Date/time components
    /// </summary>
    /// <param name="text">The text containing variables</param>
    /// <returns>The text with variables expanded</returns>
    public static string Expand(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var now = DateTime.Now;
        
        // Pattern to match {{variable}} or {{variable:format}}
        var pattern = @"\{\{([a-zA-Z]+)(?::([^}]+))?\}\}";
        
        return Regex.Replace(text, pattern, match =>
        {
            var variableName = match.Groups[1].Value.ToLowerInvariant();
            var format = match.Groups[2].Success ? match.Groups[2].Value : null;
            
            try
            {
                return variableName switch
                {
                    "date" => now.ToString(format ?? "yyyy-MM-dd"),
                    "time" => now.ToString(format ?? "HH:mm:ss"),
                    "datetime" => now.ToString(format ?? "yyyy-MM-dd HH:mm:ss"),
                    "timestamp" => new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                    "year" => now.Year.ToString(),
                    "month" => now.Month.ToString("D2"),
                    "day" => now.Day.ToString("D2"),
                    "hour" => now.Hour.ToString("D2"),
                    "minute" => now.Minute.ToString("D2"),
                    "second" => now.Second.ToString("D2"),
                    _ => match.Value // Keep original if variable not recognized
                };
            }
            catch (FormatException)
            {
                // If custom format is invalid, return the original variable
                return match.Value;
            }
        });
    }
}
