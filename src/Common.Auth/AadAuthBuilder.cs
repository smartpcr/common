// -----------------------------------------------------------------------
// <copyright file="AadAuthBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caching;
using Config;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settings;

public static class AadAuthBuilder
{
    /// <summary>
    /// Add both authentication and authorization using AAD
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns><see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddR9Auth(this IServiceCollection services, IConfiguration configuration)
    {
        var aadSettings = configuration.GetConfiguredSettings<AadSettings>();
        Console.WriteLine($"registering authentication with authority {aadSettings.Authority} and client id {aadSettings.ClientId}");

        services
            .AddAuthentication(opts =>
            {
                opts.DefaultScheme = "smart";
                opts.DefaultChallengeScheme = "smart";
            })
            .AddPolicyScheme("smart", "Authorization Bearer or OIDC",
                opts => { opts.ForwardDefaultSelector = AuthSelector(CookieAuthenticationDefaults.AuthenticationScheme); })
            .AddJwtBearer(options =>
            {
                options.Authority = aadSettings.Authority;
                options.Audience = aadSettings.ClientId;
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Query.ContainsKey("token"))
                        {
                            // graphql from hot chocolate
                            context.Token = context.HttpContext.Request.Query["token"];
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.ClientId = aadSettings.ClientId;
                options.Authority = aadSettings.Authority;
                options.UseTokenLifetime = true;
                options.CallbackPath = aadSettings.RedirectUrl?.ToString();
                options.SaveTokens = true;
                options.ForwardDefaultSelector = AuthSelector(OpenIdConnectDefaults.AuthenticationScheme);
            })
            .AddCookie(opts =>
            {
                opts.ExpireTimeSpan = TimeSpan.FromHours(1);
                opts.SlidingExpiration = false;
            });

        services.AddR9AuthSetup();

        // authorization
        services.AddAuthorization(options =>
        {
            var schemes = new List<string>
            {
                JwtBearerDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            };
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemes.ToArray())
                .Build();
        });

        return services;
    }

    public static void UseR9Auth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    /// <summary>
    /// Add caching, telemetry, logging, and http client for MSAL
    /// </summary>
    /// <param name="services"></param>
    private static void AddR9AuthSetup(this IServiceCollection services)
    {
        services
            .AddRedaction(builder => builder.SetFakeRedactor(DataClassification.Unknown))
            .AddLogging()
            .AddSingleton<MemoryCache>()
            .AddSingleton<IMemoryCache, MemoryCacheWrapper>()
            .AddSingleton(_ => TimeProvider.System);
        Console.WriteLine("registered caching, telemetry, logging, and http client for MSAL");
    }

    private static Func<HttpContext, string> AuthSelector(string fallbackScheme)
    {
        return context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            return authHeader?.StartsWith("Bearer ") == true
                ? JwtBearerDefaults.AuthenticationScheme
                : fallbackScheme;
        };
    }
}