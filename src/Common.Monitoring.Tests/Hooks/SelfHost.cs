// -----------------------------------------------------------------------
// <copyright file="SelfHost.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks;

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Common.Config;
using Common.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reqnroll;

/// <summary>
/// Start and stop a self-hosted web api for testing, make sure IServiceProvider is registered in ScenarioContext
/// </summary>
[Binding]
internal class SelfHost
{
    private readonly ScenarioContext _scenarioContext;
    private readonly WebApiHostSettings _hostSettings;
    private IHost? _host;
    private X509Certificate2? _sslCert;

    public SelfHost(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        var configuration = _scenarioContext.Get<IConfiguration>();
        _hostSettings = configuration.GetConfiguredSettings<WebApiHostSettings>();
    }

    [BeforeScenario]
    public void StartHost()
    {
        // allow enough time to debug into DI setup
        CancellationTokenSource cancelSource = new(TimeSpan.FromMinutes(5));
        _ = StartHostAsync(cancelSource.Token);
        bool isConnected = false;
        while (!cancelSource.IsCancellationRequested && !isConnected)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            isConnected = TestConnectionAsync(_hostSettings.Port, cancelSource.Token)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        if (cancelSource.IsCancellationRequested)
        {
            throw new TimeoutException("Failed to connect to self-hosted web api");
        }

        if (!isConnected)
        {
            throw new InvalidOperationException("Failed to connect to self-hosted web api");
        }

        cancelSource.Dispose();
    }

    [AfterScenario]
    public void StopHost()
    {
        CancellationTokenSource cancelSource = new(TimeSpan.FromSeconds(10));
        _ = StopHostAsync(cancelSource.Token);
        bool isConnected = true;
        while (!cancelSource.IsCancellationRequested && isConnected)
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            isConnected = TestConnectionAsync(_hostSettings.Port, cancelSource.Token)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        if (cancelSource.IsCancellationRequested)
        {
            throw new TimeoutException("Failed to terminate self-hosted web api");
        }

        if (isConnected)
        {
            throw new InvalidOperationException("Failed to terminate self-hosted web api");
        }

        cancelSource.Dispose();
    }

    private async Task StartHostAsync(CancellationToken cancel)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<Startup>(context => new Startup(_scenarioContext, context.HostingEnvironment.EnvironmentName))
                    .ConfigureKestrel(options =>
                    {
                        var serviceProvider = options.ApplicationServices;
                        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                        var hostSettings = configuration.GetConfiguredSettings<WebApiHostSettings>();
                        var hostIpAddresses = Dns.GetHostAddresses(_hostSettings.Host);
                        if (!hostIpAddresses.Any())
                        {
                            throw new InvalidOperationException($"Failed to resolve host {_hostSettings.Host}");
                        }

                        if (hostSettings.UseSsl)
                        {
                            options.Listen(hostIpAddresses.First(), _hostSettings.Port, listenOptions =>
                            {
                                listenOptions.UseHttps();
                            });
                            options.ConfigureHttpsDefaults(httpOptions =>
                            {
                                using var rsa = RSA.Create();
                                var req = new CertificateRequest($"cn={_hostSettings.Host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                _sslCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

                                httpOptions.SslProtocols = SslProtocols.Tls12;
                                httpOptions.ServerCertificate = _sslCert;
                                httpOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            });
                        }
                        else
                        {
                            options.Listen(hostIpAddresses.First(), hostSettings.Port);
                        }
                    });
            }).Build();

        await _host.RunAsync(token: cancel);
    }

    private async Task StopHostAsync(CancellationToken cancel)
    {
        if (_host != null)
        {
            await _host.StopAsync(cancel);
            _host.Dispose();
        }

        _sslCert?.Dispose();
    }

    private async Task<bool> TestConnectionAsync(int port, CancellationToken cancel)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_hostSettings.Host, port, cancel);
            return true;
        }
        catch
        {
            return false;
        }
    }
}