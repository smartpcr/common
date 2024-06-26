// -----------------------------------------------------------------------
// <copyright file="DeviceCodeAccessTokenProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Azure.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The DeviceCodeAzureServiceTokenProvider obtains user_impersonation access token using the device code auth flow.
    /// </summary>
    /// <remarks>
    /// The access token expires in 1hr. The app needs to retrieve all required secrets during startup since token refresh isn't implemented.
    /// </remarks>
    public sealed class DeviceCodeAccessTokenProvider
    {
        internal const string Empty = "";
        private readonly ILogger _logger;
        private readonly bool _isDebuggerAttached;
        private readonly IPublicClientApplication _pca;
        private readonly Func<string[], Task<AuthenticationResult>> _acquireTokenFunc;

        internal static ILogger GetLogger()
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            return factory.CreateLogger<DeviceCodeAccessTokenProvider>();
        }

        internal static string[] GetScope(string resource)
        {
            return new[] { resource + "/.default" };
        }

        internal DeviceCodeAccessTokenProvider(
            ILogger<DeviceCodeAccessTokenProvider>? logger = null,
            bool? isDebuggerAttached = null,
            IPublicClientApplication? pca = null,
            Func<string[], Task<AuthenticationResult>>? acquireTokenFunc = null,
            Func<DeviceCodeResult, Task>? emitDeviceCodeFunc = null,
            string? clientId = null)
        {
            this._logger = logger ?? GetLogger();
            this._isDebuggerAttached = isDebuggerAttached ?? Debugger.IsAttached;
            this._pca = pca ?? PublicClientApplicationBuilder.Create(clientId ?? "1950a258-227b-4e31-a9cf-717495945fc2").WithDefaultRedirectUri().Build();
            this._acquireTokenFunc = acquireTokenFunc ?? this.GetMsalAcquireTokenFunc(emitDeviceCodeFunc ?? this.EmitDeviceCodeToConsoleAsync);
        }

        [ExcludeFromCodeCoverage]
        internal Func<string[], Task<AuthenticationResult>> GetMsalAcquireTokenFunc(
            Func<DeviceCodeResult, Task> emitDeviceCodeFunc)
        {
            return scopes => this._pca.AcquireTokenWithDeviceCode(scopes, emitDeviceCodeFunc).ExecuteAsync();
        }

        internal Task EmitDeviceCodeToConsoleAsync(DeviceCodeResult deviceCodeResult)
        {
            string message = deviceCodeResult.Message;
            this._logger.LogInformation(message);
            BreakIfDebuggerIsAttached(this._isDebuggerAttached, message);
            return Task.CompletedTask;
        }

        internal AccessToken GetAccessToken(string resource)
        {
            AuthenticationResult result = this._acquireTokenFunc(GetScope(resource)).ConfigureAwait(false).GetAwaiter().GetResult();
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }

        [ExcludeFromCodeCoverage]
        private static void BreakIfDebuggerIsAttached(bool isDebuggerAttached, string message)
        {
            if (!isDebuggerAttached)
                return;
            Debugger.Log(0, string.Empty, message);
            Debugger.Break();
        }
    }
}