using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NTQQ_SignServer.Services;

public sealed class CustomConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CustomConsoleLogger(categoryName);
    public void Dispose() { }
}

internal sealed class CustomConsoleLogger : ILogger
{
    private static readonly object _lock = new object();
    private readonly string _category;
    public CustomConsoleLogger(string category) { _category = category; }
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var level = logLevel switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => logLevel.ToString().ToLowerInvariant()
        };
        var message = formatter(state, exception);
        var ts = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
        lock (_lock)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Information => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Trace => ConsoleColor.Gray,
                _ => originalColor
            };
            Console.Out.Write(level);
            Console.Out.Write(": ");
            Console.Out.Write(_category);
            Console.Out.Write("[");
            Console.Out.Write(eventId.Id.ToString());
            Console.Out.Write("]");
            Console.Out.Write(Environment.NewLine);
            Console.ForegroundColor = originalColor;
            Console.Out.Write("      ");
            Console.Out.Write(ts);
            Console.Out.Write(" ");
            Console.Out.WriteLine(message);
            if (exception != null)
            {
                Console.Out.WriteLine(exception.ToString());
            }
        }
    }
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }
}
