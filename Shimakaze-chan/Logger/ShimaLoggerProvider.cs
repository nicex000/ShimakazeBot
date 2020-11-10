using System;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace Shimakaze.Logger
{
    internal class ShimaLoggerProvider : ILoggerProvider
    {
        private LogLevel MinimumLevel { get; }
        private string TimestampFormat { get; }

        private bool _isDisposed = false;

        internal ShimaLoggerProvider(BaseDiscordClient client)
        //: this(client.Configuration.MinimumLogLevel, client.Configuration.LogTimestampFormat)
        : this()
        { }

        internal ShimaLoggerProvider(LogLevel minLevel = LogLevel.Information,
            string timestampFormat = "yyyy-MM-dd HH:mm:ss zzz")
        {
            MinimumLevel = minLevel;
            TimestampFormat = timestampFormat;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_isDisposed)
                throw new InvalidOperationException("This logger provider is already disposed.");

            if (categoryName != typeof(BaseDiscordClient).FullName)
                throw new ArgumentException("This provider can only provide instances of loggers for " +
                    $"{typeof(BaseDiscordClient).FullName}.", nameof(categoryName));

            return new ShimaLogger(MinimumLevel, TimestampFormat);
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}