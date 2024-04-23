namespace Common.Storage.Tests.Hooks;

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Reqnroll;
using Xunit;

[Binding]
public sealed class FeatureHook
{
    private static readonly List<string> beforeHookMessages = new();
    private static readonly List<string> afterHookMessages = new();

    public static List<string> DumpBeforeHookMessages()
    {
        return new List<string>(beforeHookMessages);
    }

    public static List<string> DumpAfterHookMessages()
    {
        return new List<string>(afterHookMessages);
    }

    [BeforeFeature]
    public static async Task SetupStorageEmulator()
    {
        var binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        beforeHookMessages.Add($"Bin folder: {binFolder}");
        binFolder.Should().NotBeNull();
        var startScript = Path.Combine(binFolder!, "Scripts", "StartStorageEmulator.ps1");
        beforeHookMessages.Add($"Start script: {startScript}");
        File.Exists(startScript).Should().BeTrue();

        try
        {
            using PowerShell ps = PowerShell.Create();
            ps.AddScript(startScript);
            await ps.InvokeAsync();
            beforeHookMessages.Add("Storage emulator started");
        }
        catch (RuntimeException ex)
        {
            Console.WriteLine("Powershell script failed to start storage emulator: " + ex.Message);
            beforeHookMessages.Add("Failed to start storage emulator: " + ex.Message);
            Assert.Fail("Failed to start storage emulator");
        }
    }

    [AfterFeature]
    public static async Task StopStorageEmulator()
    {
        var binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        afterHookMessages.Add($"Bin folder: {binFolder}");
        binFolder.Should().NotBeNull();
        var stopScript = Path.Combine(binFolder!, "Scripts", "StopStorageEmulator.ps1");
        afterHookMessages.Add($"Stop script: {stopScript}");
        File.Exists(stopScript).Should().BeTrue();

        try
        {
            using PowerShell ps = PowerShell.Create();
            ps.AddScript(stopScript);
            await ps.InvokeAsync();
            afterHookMessages.Add("Storage emulator stopped");
        }
        catch (RuntimeException ex)
        {
            Console.WriteLine("Powershell script failed to stop storage emulator: " + ex.Message);
            afterHookMessages.Add("Failed to stop storage emulator: " + ex.Message);
            Assert.Fail("Failed to stop storage emulator");
        }
    }
}