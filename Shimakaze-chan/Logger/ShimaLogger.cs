using System;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace Shimakaze.Logger
{
    public class ShimaLogger : ILogger<BaseDiscordClient>
    {
        private static readonly object _lock = new object();

        private LogLevel MinimumLevel { get; }
        private string TimestampFormat { get; }

        internal ShimaLogger(BaseDiscordClient client)
        //: this(client.Configuration.MinimumLogLevel, client.Configuration.LogTimestampFormat)
        : this()
        { }

        internal ShimaLogger(LogLevel minLevel = LogLevel.Information,
            string timestampFormat = "yyyy-MM-dd HH:mm:ss zzz")
        {
            MinimumLevel = minLevel;
            TimestampFormat = timestampFormat;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (_lock)
            {
                var ename = eventId.Name;
                ename = ename?.Length > 20 ? ename?.Substring(0, 20) : ename;
                Console.Write($"[{DateTimeOffset.Now.ToString(TimestampFormat)}] [{eventId.Id,-3}/ {ename,-20}] ");

                switch (logLevel)
                {
                    case LogLevel.Trace:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;

                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        break;

                    case LogLevel.Information:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;

                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;

                    case LogLevel.Critical:
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        break;
                }
                Console.Write(logLevel switch
                {
                    LogLevel.Trace => "[Trace] ",
                    LogLevel.Debug => "[Debug] ",
                    LogLevel.Information => "[Info ] ",
                    LogLevel.Warning => "[Warn ] ",
                    LogLevel.Error => "[Error] ",
                    LogLevel.Critical => "[Crit ]",
                    LogLevel.None => "[None ] ",
                    _ => "[?????] "
                });
                Console.ResetColor();

                //The foreground color is off.
                if (logLevel == LogLevel.Critical)
                    Console.Write(" ");

                var message = formatter(state, exception);
                Console.WriteLine(message);
                if (exception != null)
                    Console.WriteLine(exception);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= MinimumLevel;

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}