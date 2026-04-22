using System;

namespace Flow.Launcher.Plugin.Snippets.Util;

public enum LoggerLevel
{
    TRACE,
    DEBUG,
    INFO,
    WARN,
    ERROR,
    OFF
}

public class InnerLogger
{
    public static LoggerLevel Level { get; set; } = LoggerLevel.OFF;
    public static ILogger Logger { get; set; } = new NoneLogger();

    public static void SetAsFlowLauncherLogger(PluginInitContext context, LoggerLevel level = LoggerLevel.ERROR)
    {
        Level = level;
        Logger = new LoggerImpl(new FlowLauncherLogger(context), Level);
    }

    public static void SetAsConsoleLogger(LoggerLevel level = LoggerLevel.ERROR)
    {
        Level = level;
        Logger = new LoggerImpl(new ConsoleLogger(), Level);
    }

    public static void ResetLogger()
    {
        Logger = new NoneLogger();
    }
}

public interface ILogger
{
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception ex = null);
}

internal class LoggerImpl : ILogger
{
    private readonly ILogger _logger;
    private readonly LoggerLevel _level;

    public LoggerImpl(ILogger logger, LoggerLevel level)
    {
        _logger = logger;
        _level = level;
    }

    public void Trace(string message)
    {
        if (_level <= LoggerLevel.TRACE)
            _logger.Trace(message);
    }

    public void Debug(string message)
    {
        if (_level <= LoggerLevel.DEBUG)
            _logger.Debug(message);
    }

    public void Info(string message)
    {
        if (_level <= LoggerLevel.INFO)
            _logger.Info(message);
    }

    public void Warn(string message)
    {
        if (_level <= LoggerLevel.WARN)
            _logger.Warn(message);
    }

    public void Error(string message, Exception ex = null)
    {
        if (_level <= LoggerLevel.ERROR)
            _logger.Error(message, ex);
    }
}

internal class ConsoleLogger : ILogger
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public void Trace(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [TRACE] {message}");
    }

    public void Debug(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [DEBUG] {message}");
    }

    public void Info(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [INFO] {message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [WARN] {message}");
    }

    public void Error(string message, Exception ex = null)
    {
        if (ex != null)
        {
            Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [ERROR] {message}. cause: {ex.Message}");
        }

        Console.WriteLine($"{DateTime.Now.ToString(DateTimeFormat)} - [ERROR] {message}");
    }
}

internal class FlowLauncherLogger : ILogger
{
    private readonly PluginInitContext _context;

    public FlowLauncherLogger(PluginInitContext context)
    {
        _context = context;
    }

    public void Trace(string message)
    {
        _context.API.LogDebug(_context.CurrentPluginMetadata.Name, "Trace - " + message);
    }

    public void Debug(string message)
    {
        _context.API.LogDebug(_context.CurrentPluginMetadata.Name, message);
    }

    public void Info(string message)
    {
        _context.API.LogInfo(_context.CurrentPluginMetadata.Name, message);
    }

    public void Warn(string message)
    {
        _context.API.LogWarn(_context.CurrentPluginMetadata.Name, message);
    }

    public void Error(string message, Exception ex = null)
    {
        _context.API.LogException(_context.CurrentPluginMetadata.Name, message, ex);
    }
}

internal class NoneLogger : ILogger
{
    public void Trace(string message)
    {
    }

    public void Debug(string message)
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception ex = null)
    {
    }
}