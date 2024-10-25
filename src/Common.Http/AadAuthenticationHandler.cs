// -----------------------------------------------------------------------
// <copyright file="ServicePrincipalAuthenticationHandler.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Auth;
using EnsureThat;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;

public abstract class AadAuthenticationHandler : DelegatingHandler
{
    private readonly AsyncRetryPolicy authRetryPolicy;
    private readonly AadTokenProvider authHelper;

    /// <summary>
    /// Gets or sets aad settings specific for this service, including scopes and client id, that's different from default.
    /// </summary>
    protected abstract AadSettings AadSettings { get; set; }

    /// <summary>
    /// Max retry count when getting access token.
    /// </summary>
    public int AuthRetryCount { get; set; } = 3;

    /// <summary>
    /// Exponential backoff in seconds when getting access token.
    /// </summary>
    public int AuthRetryBackoffInSeconds { get; set; } = 5;

    protected AadAuthenticationHandler(IServiceProvider serviceProvider)
    {
        this.authHelper = new AadTokenProvider(serviceProvider);
        this.authRetryPolicy = Policy.HandleInner<SocketException>().WaitAndRetryAsync(
            AuthRetryCount,
            count => TimeSpan.FromSeconds(Math.Pow(AuthRetryBackoffInSeconds, count)));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Ensure.That(AadSettings).IsNotNull();

        return await this.authRetryPolicy.ExecuteAsync(async () =>
        {
            var correlationId = GetCorrelationIdFromRequest(request);
            var token = await this.authHelper.GetAccessTokenAsync(correlationId, cancellationToken, AadSettings.Scopes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        });
    }

    private static Guid GetCorrelationIdFromRequest(HttpRequestMessage requestMessage)
    {
        var context = Propagators.DefaultTextMapPropagator.Extract(
            default,
            requestMessage.Headers,
            (headers, name) =>
            {
                if (headers.TryGetValues(name, out var values))
                {
                    return values.ToArray();
                }

                return Array.Empty<string>();
            });
        var correlationId = context.ActivityContext.TraceId.ToHexString();
        if (correlationId == "00000000000000000000000000000000")
        {
            return Guid.NewGuid();
        }

        return Guid.Parse(correlationId);
    }
}