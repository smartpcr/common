# Usage

1. Create a POCO class to represent the configuration settings.

    ```csharp
    public class VaultSettings
    {
        [Required]
        public string VaultName { get; set; }
        public Uri VaultUrl => new Uri($"https://{VaultName}.vault.azure.net");
        public VaultAuthType AuthType { get; set; } = VaultAuthType.Msi;

        /// <summary>
        /// Gets or sets AAD settings for accessing the vault.
        /// When this is not set, it uses default AAD settings.
        /// </summary>
        public VaultAadSettings? Aad { get; set; }
    }

    public enum VaultAuthType
    {
        /// <summary>
        /// Used in production, Current app is assigned a managed identity, which is grant access to keyvault
        /// </summary>
        Msi,

        /// <summary>
        /// Used in dev environment, make sure user is authenticated via VisualStudio.
        /// or debug in IDE and set env variable DOTNET_RUNNING_IN_CONTAINER, which triggers device code flow
        /// </summary>
        User,

        /// <summary>
        /// Used in dev environment, current app is registered in aad and have access to keyvault,
        /// its access token is retrieved via client secret
        /// </summary>
        SpnWithSecretOnFile,

        /// <summary>
        /// Used in dev environment, current app is registered in aad and have access to keyvault,
        /// its access token is retrieved via client cert
        /// </summary>
        SpnWithCertOnFile
    }
    ```
2. Add it to `appsettings.json`
    ```json
    {
        "VaultSettings": {
           "VaultName": "akshci-kv-xiaodong",
           "AuthType": "Msi",
           "Aad": {
               "TenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47"
           }
       }
    }
    ```
3. Optionally override in `appsettings.Development.json`
    ```json
    {
        "VaultSettings": {
            "AuthType": "User"
        }
    }
    ```
4. To use it in your code, inject `IConfiguration` and call `GetConfiguredSettings<T>()` extension method.
    ```csharp
    // in startup.cs
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConfiguration(Configuration);
    }

    // in KeyVaultBuilder.cs
    public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
    {
        var vaultSettings = configuration.GetConfiguredSettings<VaultSettings>();
        services.AddSingleton<SecretClient>(sp =>
        {
            var tokenCredential = CreateTokenCredential(sp);
            return new SecretClient(vaultSettings.VaultUrl, tokenCredential);
        });
        services.AddSingleton<CertificateClient>(sp =>
        {
            var tokenCredential = CreateTokenCredential(sp);
            return new CertificateClient(vaultSettings.VaultUrl, tokenCredential);
        });

        return services;
    }
    ```