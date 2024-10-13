// -----------------------------------------------------------------------
// <copyright file="TestOutputHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Hooks
{
    using System;
    using System.Runtime.CompilerServices;
    using TechTalk.SpecFlow.Infrastructure;

    public static class TestOutputHelper
    {
        public static void WriteError(
            this ISpecFlowOutputHelper outputHelper,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            outputHelper.WriteLine($"ERROR: {DateTime.UtcNow:u} [{filePath}.{memberName}.{lineNumber}] {message}");
            Console.ResetColor();
        }

        public static void WriteWarning(
            this ISpecFlowOutputHelper outputHelper,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            outputHelper.WriteLine($"WARN: {DateTime.UtcNow:u} [{filePath}.{memberName}.{lineNumber}] {message}");
            Console.ResetColor();
        }

        public static void WriteInfo(
            this ISpecFlowOutputHelper outputHelper,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            outputHelper.WriteLine($"INFO: {DateTime.UtcNow:u} [{filePath}.{memberName}.{lineNumber}] {message}");
            Console.ResetColor();
        }

        public static void WriteVerbose(
            this ISpecFlowOutputHelper outputHelper,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            outputHelper.WriteLine($"INFO: {DateTime.UtcNow:u} [{filePath}.{memberName}.{lineNumber}] {message}");
            Console.ResetColor();
        }
    }
}