// -----------------------------------------------------------------------
// <copyright file="StaticTokenCredential.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;

    internal sealed class StaticTokenCredential : TokenCredential
    {
        private readonly AccessToken _accessToken;

        internal StaticTokenCredential(string token, TimeProvider clock)
            : this(new AccessToken(token, clock.GetUtcNow().UtcDateTime.AddHours(1.0)))
        {
        }

        internal StaticTokenCredential(AccessToken accessToken) => this._accessToken = accessToken;

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return this._accessToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(this._accessToken);
        }
    }
}