// -----------------------------------------------------------------------
// <copyright file="MockedLoggerFactory.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Hooks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;

    public sealed class MockedLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName) => new MockedLogger(categoryName);

        public ILogger<T> CreateLogger<T>() => new MockedLogger<T>();

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }

    public class MockedLogger<T> : ILogger<T>
    {
        public string CategoryName => typeof(T).FullName ?? string.Empty;
        public List<(LogLevel level, string message)> Logs { get; } = new List<(LogLevel level, string message)>();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                Logs.Add((logLevel, message));
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }

    public class MockedLogger : ILogger
    {
        public string CategoryName { get; }
        public List<(LogLevel level, string message)> Logs { get; } = new List<(LogLevel level, string message)>();

        public MockedLogger(string categoryName)
        {
            CategoryName = categoryName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                Logs.Add((logLevel, message));
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }
    }
}