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
using Auth;
using EnsureThat;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;
using Settings;

public abstract class ServicePrincipalAuthenticationHandler : DelegatingHandler
{
    private readonly IServiceProvider serviceProvider;
    private readonly AsyncRetryPolicy retryPolicy;

    /// <summary>
    /// Gets or sets aad settings specific for this service, including scopes and client id, that's different from default.
    /// </summary>
    protected abstract AadSettings? AadSettings { get; set; }

    protected ServicePrincipalAuthenticationHandler(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        retryPolicy = Policy.HandleInner<SocketException>().WaitAndRetryAsync(2, count => TimeSpan.FromSeconds(Math.Pow(4, count)));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Ensure.That(AadSettings).IsNotNull();
        var authHelper = new AadTokenProvider(serviceProvider);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var correlationId = GetCorrelationIdFromRequest(request);
            var token = await authHelper.GetAccessTokenAsync(correlationId, cancellationToken, AadSettings?.Scopes);
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