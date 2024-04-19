// -----------------------------------------------------------------------
// <copyright file="MethodRequestMetricEnricher.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Enrichment;

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.R9.Extensions.Enrichment;
using Microsoft.R9.Service.Middleware;

internal class MethodRequestMetricEnricher : IIncomingRequestMetricEnricher
{
    private const string RequestMethod = "req_method";
    private readonly IHttpContextAccessor _httpContextAccessor;
    public IReadOnlyList<string> DimensionNames => new[] { RequestMethod };

    public MethodRequestMetricEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        enrichmentBag.Add(RequestMethod, _httpContextAccessor?.HttpContext?.Request?.Method ?? "[null]");
    }
}