using System;
using System.Collections.Generic;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace Shimakaze.Logger
{
    internal class ShimaLoggerFactory : ILoggerFactory
    {
        private List<ILoggerProvider> Providers { get; } = new List<ILoggerProvider>();
        private bool _isDisposed = false;

        public void AddProvider(ILoggerProvider provider)
        {
            Providers.Add(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_isDisposed)
                throw new InvalidOperationException("This logger factory is already disposed.");

            if (categoryName != typeof(BaseDiscordClient).FullName)
                throw new ArgumentException($"This factory can only provide instances of loggers for {typeof(BaseDiscordClient).FullName}.", nameof(categoryName));

            return new CompositeShimaLogger(Providers);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            foreach (var provider in Providers)
                provider.Dispose();

            Providers.Clear();
        }
    }
}