namespace Common.Storage.Tests.Hooks;

using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Reqnroll;
using Xunit.Abstractions;

[Binding]
public sealed class ScenarioHook
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private readonly ITestOutputHelper _output;
    private readonly ScenarioContext _context;

    public ScenarioHook(ITestOutputHelper output, ScenarioContext context)
    {
        _output = output;
        _context = context;
    }

    [BeforeScenario]
    public void AssertStorageEmulatorRunning()
    {
        _output.WriteLine(FeatureHook.DumpBeforeHookMessages().Aggregate((a, b) => a + "\n" + b));
        _output.WriteLine("Checking if storage emulator is running");
        var proc = Process.GetProcesses()
            .FirstOrDefault(p => p.ProcessName.Contains("StorageEmulator"));
        _output.WriteLine(proc != null ? "Storage emulator is running" : "Storage emulator is not running");
        proc.Should().NotBeNull();

        var account = CloudStorageAccount.Parse(ConnectionString);
        var client = account.CreateCloudBlobClient();
        _context.Set(client, "BlobClient");
    }
}