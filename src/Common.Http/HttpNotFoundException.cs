// -----------------------------------------------------------------------
// <copyright file="HttpNotFoundException.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;

public class HttpNotFoundException : Exception
{
    public HttpNotFoundException(string message) : base(message) { }
}