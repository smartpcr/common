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
using Config.Tests.Hooks;
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

    public SelfHost(ScenarioContext scenarioContext, FeatureContext featureContext, ISpecFlowOutputHelper outputWriter)
    {
        this.scenarioContext = scenarioContext;
        this.featureContext = featureContext;
        this.outputWriter = outputWriter;
        this.listener = new HttpListener()
        {
            IgnoreWriteExceptions = true,
            AuthenticationSchemes = AuthenticationSchemes.Anonymous,
        };
    }

    [BeforeScenario]
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
        if (string.IsNullOrEmpty(requestPath))
        {
            this.outputWriter.WriteError($"[{this.GetType().Name}] Request path is empty");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        if (this.scenarioContext.TryGetValue($"{context.Request.HttpMethod} {requestPath}", out Func<HttpListenerContext, Task> handler))
        {
            try
            {
                await handler(context);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                this.outputWriter.WriteError($"[{this.GetType().Name}] Error handling request: {ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // set the response content to the exception message
                context.Response.ContentType = "text/plain";
                await context.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(ex.Message));
            }
        }
        else
        {
            this.outputWriter.WriteError($"[{this.GetType().Name}] No handler found for {context.Request.HttpMethod} {requestPath}");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        await context.Response.OutputStream.FlushAsync();
        context.Response.Close();
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