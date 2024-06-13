// -----------------------------------------------------------------------
// <copyright file="OperationNotPermittedException.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;

public class OperationNotPermittedException : Exception
{
    public OperationNotPermittedException()
    {
    }

    public OperationNotPermittedException(string message) : base(message)
    {
    }

    public OperationNotPermittedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}