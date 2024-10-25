// -----------------------------------------------------------------------
// <copyright file="AadAuthScenarios.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth;

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