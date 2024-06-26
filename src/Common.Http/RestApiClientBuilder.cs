// -----------------------------------------------------------------------
// <copyright file="RestApiClientBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;
using System.Net;
using System.Net.Http;
using Config;
using EnsureThat;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;
using Settings;

public static class RestApiClientBuilder
{
    /// <summary>
    /// Registers a REST API client with aad auth and execute policy.
    /// </summary>
    /// <typeparam name="TClient">The interface representing the REST API client.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation of the REST API client.</typeparam>
    /// <typeparam name="TAuthHandler">The authentication handler type for the REST API client.</typeparam>
    /// <param name="services">The service collection to register the REST API client with.</param>
    /// <param name="apiClientSettingName">The setting name to retrieve the API client configuration from.</param>
    public static IServiceCollection AddRestApiClient<TClient, TImplementation, TAuthHandler>(this IServiceCollection services, string apiClientSettingName)
        where TClient : class
        where TImplementation : class, TClient
        where TAuthHandler : AadAuthenticationHandler
    {
        services.TryAddTransient<TImplementation>();
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var serviceSettings = configuration.GetConfiguredSettings<ApiClientSettings>(apiClientSettingName);
        var clientBuilder = services.AddHttpClient<TClient, TImplementation>((_, client) =>
        {
            serviceSettings.Configure(client, metadata.ApplicationName);
        });

        if (serviceSettings.AuthMode != HttpClientAuthMode.None)
        {
            clientBuilder.AddHttpMessageHandler<TAuthHandler>();
        }

        if (serviceSettings.Timeout != default)
        {
            clientBuilder.SetHandlerLifetime(serviceSettings.Timeout);
        }

        if (serviceSettings.RetryCount > 1)
        {
            clientBuilder.AddPolicyHandler(GetRetryPolicy(serviceSettings.RetryCount));
        }

        if (serviceSettings.CircuitBreakCount > 0)
        {
            clientBuilder.AddPolicyHandler(GetCircuitBreakerPolicy(serviceSettings.CircuitBreakCount));
        }

        if (serviceSettings.Proxy != null)
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                Proxy = new WebProxy
                {
                    Address = serviceSettings.Proxy.ProxyUri,
                    BypassProxyOnLocal = serviceSettings.Proxy.BypassProxyOnLocal
                }
            });
        }

        return services;
    }

    /// <summary>
    /// Extension method to create a REST API client without authentication.
    /// </summary>
    /// <typeparam name="TClient">The interface of the REST API client.</typeparam>
    /// <typeparam name="TImplementation">The implementation of the REST API client.</typeparam>
    /// <param name="services">The IServiceCollection instance.</param>
    /// <param name="endpoint">The base URL of the REST API.</param>
    /// <param name="timeout">Optional. The timeout for HTTP requests. Defaults to 10 seconds.</param>
    public static void AddRestApiClient<TClient, TImplementation>(this IServiceCollection services, string endpoint, TimeSpan timeout = default)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((_, client) =>
        {
            client.BaseAddress = new Uri(endpoint);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = timeout == default ? TimeSpan.FromSeconds(10) : timeout;
        })
        .SetHandlerLifetime(timeout == default ? TimeSpan.FromSeconds(10) : timeout);
    }

    private static void Configure(this ApiClientSettings settings, HttpClient client, string applicationName)
    {
        Ensure.That(settings).IsNotNull();
        Ensure.That(client).IsNotNull();
        Ensure.That(settings.Endpoint).IsNotNullOrWhiteSpace();

        client.BaseAddress = new Uri(settings.Endpoint);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", applicationName);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int breakOnCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(breakOnCount, TimeSpan.FromSeconds(30));
    }
}