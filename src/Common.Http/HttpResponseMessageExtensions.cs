// -----------------------------------------------------------------------
// <copyright file="HttpResponseMessageExtensions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class HttpResponseMessageExtensions
{
    public static async Task<T?> ReadAsync<T>(this HttpResponseMessage httpResponse, CancellationToken cancel)
    {
        var content = await GetContentAsync(httpResponse, cancel);

        if (IsHttpResponseSuccess(httpResponse))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                if (content.Length > 5000)
                {
                    content = content[..5000] + "...";
                }

                throw new InvalidOperationException($"Error deserializing {typeof(T)} from: {content}", ex);
            }
        }

        CaptureKnownExceptionsOnFailure(httpResponse.StatusCode, content);
        throw new InvalidOperationException(content);
    }

    public static async Task<JObject> ReadJObject(this HttpResponseMessage httpResponse, CancellationToken cancel)
    {
        var content = await GetContentAsync(httpResponse, cancel);

        if (IsHttpResponseSuccess(httpResponse))
        {
            return JObject.Parse(content);
        }

        CaptureKnownExceptionsOnFailure(httpResponse.StatusCode, content);
        throw new InvalidOperationException(content);
    }

    private static bool IsHttpResponseSuccess(HttpResponseMessage? httpResponse)
    {
        return httpResponse is { IsSuccessStatusCode: true };
    }

    private static Task<string> GetContentAsync(HttpResponseMessage httpResponse, CancellationToken cancel)
    {
        return httpResponse.Content.ReadAsStringAsync(cancel);
    }

    private static void CaptureKnownExceptionsOnFailure(HttpStatusCode statusCode, string messageContent)
    {
        switch (statusCode)
        {
            case HttpStatusCode.NotFound:
                throw new HttpNotFoundException(messageContent);
            case HttpStatusCode.Forbidden:
                throw new OperationNotPermittedException(messageContent);
            case HttpStatusCode.Conflict:
                throw new ConflictException(messageContent);
        }
    }
}