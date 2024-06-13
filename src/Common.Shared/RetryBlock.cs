// -----------------------------------------------------------------------
// <copyright file="RetryBlock.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public static class RetryBlock
{
    public static async Task RetryOnThrottling(int times, TimeSpan delay, Func<Task> operation, ILogger logger, Predicate<Exception>? exceptionFilter = null)
    {
        var attempts = 0;
        do
        {
            try
            {
                attempts++;
                await operation();
                break; // success
            }
            catch (Exception ex)
            {
                if ((exceptionFilter?.Invoke(ex) == true || exceptionFilter == null) && attempts < times)
                {
                    logger?.RetryErrorContinue(attempts, ex.Message, ex.StackTrace ?? "");
                    await Task.Delay(delay);
                }
                else
                {
                    logger?.RetryErrorStop(attempts, ex.Message, ex.StackTrace ?? "");
                    throw;
                }
            }
        }
        while (true);
    }

    public static void Retry(int times, TimeSpan delay, Action operation, ILogger logger, Predicate<Exception>? exceptionFilter = null)
    {
        var attempts = 0;
        do
        {
            try
            {
                attempts++;
                operation();
                break; // success
            }
            catch (Exception ex)
            {
                if ((exceptionFilter?.Invoke(ex) == true || exceptionFilter == null) && attempts < times)
                {
                    logger?.RetryErrorContinue(attempts, ex.Message, ex.StackTrace ?? "");
                    Thread.Sleep(delay);
                }
                else
                {
                    logger?.RetryErrorStop(attempts, ex.Message, ex.StackTrace ?? "");
                    throw;
                }
            }
        }
        while (true);
    }
}