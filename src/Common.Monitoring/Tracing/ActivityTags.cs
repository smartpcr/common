// -----------------------------------------------------------------------
// <copyright file="ActivityTags.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tracing;

public static class ActivityTags
{
    public const string HttpMethod = "http.method";
    public const string HttpUrl = "http.url";
    public const string HttpPathKey = "http.path";

    public const string RequestProtocol = "request.protocol";
    public const string ResponseLength = "response.length";

    public const string StatusCodeKey = "otel.status_code";
    public const string StatusDescriptionKey = "otel.status_description";

    public const string DatabaseStatementTypeKey = "db.statement_type";

    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";
    public const string ExceptionStackTrace = "exception.stacktrace";
}