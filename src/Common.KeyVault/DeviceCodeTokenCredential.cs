// -----------------------------------------------------------------------
// <copyright file="DeviceCodeTokenCredential.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;

    internal sealed class DeviceCodeTokenCredential : TokenCredential
    {
        private readonly Lazy<AccessToken> _accessToken;

        internal DeviceCodeTokenCredential(
            string resource,
            Func<DeviceCodeAccessTokenProvider>? deviceCodeAccessTokenProviderFactory = null)
        {
            if (deviceCodeAccessTokenProviderFactory == null)
                deviceCodeAccessTokenProviderFactory = (Func<DeviceCodeAccessTokenProvider>) (() => new DeviceCodeAccessTokenProvider());
            this._accessToken = new Lazy<AccessToken>(() => deviceCodeAccessTokenProviderFactory().GetAccessToken(resource), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return this._accessToken.Value;
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(this._accessToken.Value);
        }
    }
}