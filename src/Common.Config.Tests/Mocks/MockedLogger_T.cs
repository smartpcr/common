// -----------------------------------------------------------------------
// <copyright file="MockedLogger_T.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Mocks;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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