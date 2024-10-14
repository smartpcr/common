// -----------------------------------------------------------------------
// <copyright file="SelfHost.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Config;
using Common.Monitoring.Tests.Utils;
using Config.Tests.Hooks;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

/// <summary>
/// Start and stop a self-hosted web api for testing, make sure IServiceProvider is registered in ScenarioContext
/// </summary>
[Binding]
internal class SelfHost
{
    private readonly ScenarioContext scenarioContext;
    private readonly FeatureContext featureContext;
    private readonly ISpecFlowOutputHelper outputWriter;
    private readonly HttpListener listener;
    private bool running;

    private readonly ILogger<SelfHost> logger;
    private readonly ApiRequestMetric apiRequestMetric;
    private readonly Tracer tracer;
    private readonly MeterProvider meterProvider;

    public SelfHost(ScenarioContext scenarioContext, FeatureContext featureContext, ISpecFlowOutputHelper outputWriter)
    {
        this.scenarioContext = scenarioContext;
        this.featureContext = featureContext;
        this.outputWriter = outputWriter;

        var serviceProvider = this.scenarioContext.Get<IServiceProvider>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        this.logger = loggerFactory.CreateLogger<SelfHost>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        this.apiRequestMetric = ApiRequestMetric.Instance(metadata);
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        this.tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(SelfHost)}", metadata.BuildVersion);
        this.meterProvider = serviceProvider.GetRequiredService<MeterProvider>();

        this.listener = new HttpListener()
        {
            IgnoreWriteExceptions = true,
            AuthenticationSchemes = AuthenticationSchemes.Anonymous,
        };
    }

    [BeforeScenario(Order = 3)]
    public void StartHost()
    {
        if (!this.featureContext.TryGetValue("UsedPorts", out List<int> usedPorts))
        {
            usedPorts = new List<int>();
            this.featureContext.Set(usedPorts, "UsedPorts");
        }
        var uri = $"http://{Dns.GetHostName()}:{GetUnusedPort(usedPorts)}/";

        this.listener.Prefixes.Add(uri);
        this.listener.Start();
        this.running = true;
        this.outputWriter.WriteLine($"started web api at {uri}");
        this.scenarioContext.Set(uri, "WebApiUri");

        // start processing requests
        _ = this.ProcessRequests();
        this.outputWriter.WriteInfo($"web api started listening at {uri}");
        this.logger.Log(LogLevel.Information, $"web api started listening at {uri}");

        var timeoutSeconds = Debugger.IsAttached
            ? 120
            : 1;
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{uri}"),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        this.scenarioContext.Set(httpClient); // automatically disposed after test
    }

    [AfterScenario]
    public void StopHost()
    {
        this.listener.Stop();
        this.listener.Close();
        this.running = false;
        this.outputWriter.WriteInfo("web api stopped");
        this.logger.Log(LogLevel.Information, "web api stopped");
    }

    private async Task ProcessRequests()
    {
        while (this.running)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await this.listener.GetContextAsync();
            }
            catch (ObjectDisposedException) { }
            catch (HttpListenerException) { }

            if (context != null)
            {
                await this.RespondToRequest(context);
            }
        }
    }

    private async Task RespondToRequest(HttpListenerContext context)
    {
        var requestPath = context.Request.Url?.LocalPath;
        this.logger.StartingApiCall(DateTime.Now, requestPath ?? string.Empty);
        this.apiRequestMetric.IncrementTotalRequests();
        using var span = this.tracer.StartActiveSpan(nameof(this.RespondToRequest));
        var watch = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(requestPath))
        {
            this.outputWriter.WriteError($"[{this.GetType().Name}] Request path is empty");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            this.apiRequestMetric.IncrementFailedRequests();
            return;
        }

        if (this.scenarioContext.TryGetValue($"{context.Request.HttpMethod} {requestPath}", out Func<HttpListenerContext, Task> handler))
        {
            try
            {
                await handler(context);
                this.apiRequestMetric.IncrementSuccessfulRequests();
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                this.apiRequestMetric.IncrementFailedRequests();
                this.outputWriter.WriteError($"[{this.GetType().Name}] Error handling request: {ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // set the response content to the exception message
                context.Response.ContentType = "text/plain";
                await context.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(ex.Message));
                this.logger.ApiCallFailed(DateTime.Now, requestPath, watch.ElapsedMilliseconds, ex.Message);
                span.SetStatus(Status.Error);
                span.RecordException(ex);
            }
            finally
            {
                this.apiRequestMetric.RecordRequestLatency(watch.ElapsedMilliseconds);
                this.logger.ApiCallCompleted(DateTime.Now, requestPath, watch.ElapsedMilliseconds);
            }
        }
        else
        {
            this.outputWriter.WriteError($"[{this.GetType().Name}] No handler found for {context.Request.HttpMethod} {requestPath}");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        await context.Response.OutputStream.FlushAsync();
        context.Response.Close();

        this.meterProvider.ForceFlush();
    }

    private static int GetUnusedPort(List<int> usedPorts)
    {
        var random = new Random();
        TcpListener? tcpListener = null;

        while (true)
        {
            // note: all listener requires admin privilege to bind to port other than 80
            var port = random.Next(49152, 65535);
            if (usedPorts.Contains(port))
            {
                continue;
            }

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                usedPorts.Add(port);
                return port;
            }
            catch (SocketException)
            {
                // Port is in use, try another one
            }
            finally
            {
                tcpListener?.Stop();
            }
        }
    }
}