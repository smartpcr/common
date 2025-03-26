// -----------------------------------------------------------------------
// <copyright file="BlobStorageHook.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache.Tests.Hooks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Reqnroll;
    using Xunit;

    [Binding]
    public class BlobStorageHook
    {
        internal static readonly string LocalStorageConnection = "UseDevelopmentStorage=true";

        [BeforeFeature("blob", Order = 10)]
        public static void SetupBlobStorageSimulator(FeatureContext context)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Storage emulator is only supported on Windows platform, skipping storage emulator setup.");
                Assert.Fail("Test can only run in windows box");
            }

            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            var isAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                Console.WriteLine("Storage emulator requires admin privilege, skipping storage emulator setup.");
                Assert.Fail("Setup requires admin privilege.");
            }

            var installEmulatorScriptFile =
                Path.Combine(Directory.GetCurrentDirectory(), @"Hooks\Blobs\StartStorageEmulator.ps1");
            if (!File.Exists(installEmulatorScriptFile))
            {
                Console.WriteLine($"Storage emulator setup script not found at {installEmulatorScriptFile}");
                Assert.Fail("Storage emulator setup script not found.");
            }

            InvokePowerShellScript(installEmulatorScriptFile);

            var account = CloudStorageAccount.Parse(LocalStorageConnection);
            var blobClient = account.CreateCloudBlobClient();
            context.Set(blobClient, "BlobClient");
            var container = blobClient.GetContainerReference("testcontainer");
            container.CreateIfNotExists();
            context.Set(container, "BlobContainer");
        }

        [AfterFeature("blob", Order = 10)]
        public static void ShutdownBlobStorageSimulator(FeatureContext context)
        {
            if (!context.FeatureInfo.Tags.Contains("blob"))
            {
                Console.WriteLine("Feature does not contain 'blob' tag, skipping storage emulator shutdown.");
                return;
            }

            var blobClient = context.Get<CloudBlobClient>("BlobClient");
            IEnumerable<CloudBlobContainer> containers = blobClient.ListContainers();
            foreach (var container in containers)
            {
                container.DeleteIfExists();
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Storage emulator is only supported on Windows platform, skipping storage emulator shutdown.");
                Assert.Fail("Test can only run in windows box");
            }

            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            var isAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                Console.WriteLine("Storage emulator requires admin privilege, skipping storage emulator shutdown.");
                Assert.Fail("Setup requires admin privilege.");
            }

            var uninstallEmulatorScriptFile =
                Path.Combine(Directory.GetCurrentDirectory(), @"Hooks\Blobs\StopStorageEmulator.ps1");
            if (!File.Exists(uninstallEmulatorScriptFile))
            {
                Console.WriteLine($"Storage emulator remove script not found at {uninstallEmulatorScriptFile}");
                Assert.Fail("Storage emulator remove script not found.");
            }

            InvokePowerShellScript(uninstallEmulatorScriptFile);
        }

        private static void InvokePowerShellScript(string scriptFile)
        {
            using var ps = PowerShell.Create();
            Collection<PSObject>? results = null;
            try
            {
                ps.AddScript(scriptFile);

                ps.Streams.Error.DataAdded += (sender, e) =>
                {
                    if (sender is PSDataCollection<ErrorRecord> records)
                    {
                        var record = records[e.Index];
                        Console.WriteLine("Error: " + record.Exception.Message);
                    }
                };
                ps.Streams.Warning.DataAdded += (sender, e) =>
                {
                    if (sender is PSDataCollection<WarningRecord> records)
                    {
                        var record = records[e.Index];
                        Console.WriteLine("Warning: " + record.Message);
                    }
                };
                ps.Streams.Debug.DataAdded += (sender, e) =>
                {
                    if (sender is PSDataCollection<DebugRecord> records)
                    {
                        var record = records[e.Index];
                        Console.WriteLine("Debug: " + record.Message);
                    }
                };
                ps.Streams.Verbose.DataAdded += (sender, e) =>
                {
                    if (sender is PSDataCollection<VerboseRecord> records)
                    {
                        var record = records[e.Index];
                        Console.WriteLine("Verbose: " + record.Message);
                    }
                };
                results = ps.Invoke();
            }
            catch (RuntimeException ex)
            {
                Assert.Fail($"Failed to run PowerShell script {scriptFile}, runtime error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to run PowerShell script {scriptFile}, unhandled error: {ex.Message}");
            }
            finally
            {
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        Console.WriteLine("Output: " + result);
                    }
                }

                if (ps.Streams?.Error?.Count > 0)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Console.WriteLine("Error: " + error);
                    }
                }
            }
        }
    }
}
