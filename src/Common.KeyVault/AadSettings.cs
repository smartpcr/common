// -----------------------------------------------------------------------
// <copyright file="AadSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System;
using System.ComponentModel.DataAnnotations;

public class AadSettings
{
    /// <summary>
    /// TenantId for Microsoft AAD. See also <see cref="MicrosoftAadTenantId" />
    /// </summary>
    public const string MicrosoftAadTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

    /// <summary>
    /// TenantId for AME tenant, this is used for first party applications inside Microsoft..
    /// </summary>
    public const string AMEAadTenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";

    /// <summary>
    /// TenantId for GME tenant, this is used for first party applications inside Microsoft.
    /// </summary>
    public const string GMEAadTenantId = "124edf19-b350-4797-aefc-3206115ffdb3";

    /// <summary>
    /// TenantId for PME tenant, PME stands for Public Managed Environment. It's an isolated, hardened environment
    /// used to run many of Microsoft's non-core production services in C+AI.
    /// </summary>
    public const string PMEAadTenantId = "975f013f-7f24-47e8-a7d3-abc4752bf346";

    public const string MicrosoftAadLoginUrl = "https://login.microsoftonline.com/";

    /// <summary>
    /// Gets or sets the AAD tenant login URL. See also <see cref="MicrosoftAadLoginUrl" />
    /// </summary>
    public string Instance { get; set; } = MicrosoftAadLoginUrl;

    /// <summary>
    /// Gets or sets the AAD tenant. See also <see cref="MicrosoftAadTenantId" />
    /// </summary>
    [Required]
    public string TenantId { get; set; }

    /// <summary>
    /// Gets or sets the ClientId. Note that ClientId is obsolete name for ApplicationId.
    /// </summary>
    [Required]
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets represents the authentication settings for Azure Active Directory (AAD).
    /// For confidential app, this is specified on permissions tab of the application registration in AAD.
    /// For public app, this is usually well-known scopes such as https://graph.microsoft.com/user.read.
    /// </summary>
    public string[]? Scopes { get; set; }

    /// <summary>
    /// Gets represents the Azure Active Directory (AAD) settings for authentication and authorization.
    /// In the context of authentication, particularly with OAuth 2.0 and OpenID Connect,
    /// the term "authority" refers to the URL of the authorization server that issues tokens
    /// In Azure Active Directory (AAD), the authority URL might look like
    /// https://login.microsoftonline.com/{tenantId} where tenantId is the AAD tenant ID.
    /// https://login.microsoftonline.com/common for multi-tenant applications
    /// For Google's OAuth 2.0 services, the authority URL is https://accounts.google.com.
    /// </summary>
    /// <remarks>
    /// The AadSettings class contains properties for configuring the AAD authentication and authorization settings.
    /// </remarks>
    public string Authority => $"{Instance}{TenantId}";

    /// <summary>
    /// Gets or sets the redirect url for the AAD authentication middleware.
    /// </summary>
    /// <remarks>
    /// The redirect url is the path in the application's URL space where the middleware will listen for the response from AAD after a successful authentication.
    /// This should match the redirect URI configured in the AAD application registration.
    /// In the case for user interactive authentication, callback is http://localhost where the browser is redirected to after successful authentication.
    /// For testing web api running on local with port 5000, callback is http://localhost:5000/signin-oidc
    /// </remarks>
    public Uri? RedirectUrl { get; set; }

    /// <summary>
    /// Gets or sets usage scenarios for AAD authentication.
    /// </summary>
    public AadAuthScenarios Scenarios { get; set; }

    /// <summary>
    /// Gets or sets represents the source of the client secret for authentication.
    /// </summary>
    public AadClientSecretSource ClientSecretSource { get; set; }

    /// <summary>
    /// Gets or sets secret based on ClientSecretSource
    /// None (managed identity or interactive user): this is not used.
    /// ClientSecretFromFile: this is the path to the file that contains the client secret.
    /// ClientCertFromFile: this is the path to the file that contains the client certificate.
    /// ClientSecretFromVault: secret name in key vault.
    /// ClientCertFromVault: certificate name in key vault.
    /// </summary>
    public string ClientSecretName { get; set; }
}

public enum AadAuthScenarios
{
    /// <summary>
    /// Usage: For applications that can securely hold a client secret or a certificate
    /// Scenario: Web apps, web APIs, background services, or daemons that need to access resources or
    /// APIs on behalf of themselves (not on behalf of a user).
    /// Methods: Acquire tokens using client credentials or on-behalf-of flow.
    /// </summary>
    ConfidentialApp,

    /// <summary>
    /// Usage: For applications that cannot securely store a client secret or certificate, such as client-side applications.
    /// Scenarios: Desktop applications, mobile applications, or single-page applications (SPAs)
    /// where the authentication process involves user interaction.
    /// Methods: Acquire tokens interactively or using integrated Windows authentication, device code flow, or username/password.
    /// </summary>
    PublicApp,

    /// <summary>
    /// Requires user interaction to log in (common in public client applications).
    /// </summary>
    InteractiveUser,

    /// <summary>
    /// Usage: For Azure services that need to authenticate to other Azure services securely without storing credentials in code.
    /// Scenarios: Azure Functions, Azure App Services, or any Azure service that supports Managed Identity,
    /// interacting with Azure resources like Azure SQL Database, Blob Storage, etc.
    /// </summary>
    ManagedIdentity,

    /// <summary>
    /// Usage: For devices that do not have a browser or have limited input capabilities.
    /// Scenarios: IoT devices, CLI tools, or any application running on a device with constrained UI.
    /// </summary>
    DeviceCodeFlow,

    /// <summary>
    /// Usage: In scenarios where a service needs to call another service on behalf of a user.
    /// Scenarios: Middle-tier services, where the service needs to perform actions by impersonating the user.
    /// </summary>
    OnBehalfOf,

    /// <summary>
    /// Usage: For applications using Azure AD B2C, tailored for customer-facing applications.
    /// Scenarios: Consumer applications where end-users can sign in with various identities (like social accounts, local accounts).
    /// </summary>
    BusinessToConsumer
}

public enum AadClientSecretSource
{
    /// <summary>
    /// Either using managed identity or interactive user login
    /// </summary>
    None,

    /// <summary>
    /// Using SPN with client secret that's stored in local file, usually used for local debug
    /// </summary>
    ClientSecretFromFile,

    /// <summary>
    /// Using SPN with certificate that's stored in local file, usually used for local debug
    /// </summary>
    ClientCertFromFile,

    /// <summary>
    /// Using SPN with client secret stored in key vault, key vault authentication is managed separately
    /// </summary>
    ClientSecretFromVault,

    /// <summary>
    /// Using SPN with certificate that is stored in key vault, key vault authentication is managed separately
    /// </summary>
    ClientCertFromVault
}