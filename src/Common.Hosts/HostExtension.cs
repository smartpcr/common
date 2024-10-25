// -----------------------------------------------------------------------
// <copyright file="HostExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class HostExtension
{
    public static void ConfigureKestrel(this IServiceProvider serviceProvider, KestrelServerOptions options)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var hostSettings = configuration.GetConfiguredSettings<WebApiHostSettings>();

        if (hostSettings.UseSsl)
        {
            var certFile = hostSettings.CertFile;
            if (!File.Exists(certFile))
            {
                var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                certFile = Path.Combine(homeFolder, ".secrets", certFile);
            }

            if (!File.Exists(certFile))
            {
                throw new InvalidOperationException($"Failed to find cert file {hostSettings.CertFile}");
            }

            options.Listen(IPAddress.Parse(hostSettings.Host), hostSettings.Port, listenOptions =>
            {
                listenOptions.UseHttps();
            });
            options.ConfigureHttpsDefaults(httpOptions =>
            {
                var sslCert = new X509Certificate2(certFile);
                httpOptions.SslProtocols = SslProtocols.Tls12;
                httpOptions.ServerCertificate = sslCert;
                httpOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            });
        }
        else
        {
            options.Listen(IPAddress.Parse(hostSettings.Host), hostSettings.Port);
        }
    }
}