// -----------------------------------------------------------------------
// <copyright file="ISecretProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public interface ISecretProvider
{
    Task<IList<string>> ListSecretsAsync(CancellationToken cancel);

    Task<string> GetSecretAsync(string secretName, CancellationToken cancel);

    string GetSecret(string secretName);

    Task<X509Certificate2> GetCertAsync(string certName, CancellationToken cancel);

    X509Certificate2 GetCert(string certName);
}