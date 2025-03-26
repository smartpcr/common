namespace Common.KeyVault.Tests.Hooks;

using Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monitoring;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

[Binding]
public sealed class SetupKeyVault
{
    private readonly ScenarioContext context;
    private readonly IReqnrollOutputHelper outputHelper;

    public SetupKeyVault(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
    {
        this.context = scenarioContext;
        this.outputHelper = outputHelper;
    }

    [ScenarioDependencies]
    public static IServiceCollection GetServiceCollection()
    {
        return new ServiceCollection();
    }

    [TechTalk.SpecFlow.BeforeScenario("User")]
    public void SetupUserAuth()
    {
        SetupVautAuthType(VaultAuthType.User);
    }

    [TechTalk.SpecFlow.BeforeScenario("Msi")]
    public void SetupMsiAuth()
    {
        SetupVautAuthType(VaultAuthType.Msi);
    }

    [TechTalk.SpecFlow.BeforeScenario("ClientSecret")]
    public void SetupSpnWithClientSecretsAuth()
    {
        SetupVautAuthType(VaultAuthType.SpnWithSecretOnFile);
    }

    [TechTalk.SpecFlow.BeforeScenario("ClientCertificate")]
    public void SetupSpnWithClientCertificateAuth()
    {
        SetupVautAuthType(VaultAuthType.SpnWithCertOnFile);
    }

    private void SetupVautAuthType(VaultAuthType authType)
    {
        var configuration = TryGetConfiguration(context);
        var vaultSettings = configuration.GetConfiguredSettings<VaultSettings>();
        vaultSettings.AuthType = authType;
        var services = TryGetServices(context);
        services.Configure<VaultSettings>(options =>
        {
            options.AuthType = authType;
            options.VaultName = vaultSettings.VaultName;
            options.Aad = vaultSettings.Aad;
        });
        context.Set(vaultSettings);
        outputHelper.WriteLine($"Use vault auth type: {vaultSettings.AuthType}");
    }

    internal static IServiceCollection TryGetServices(ScenarioContext scenarioContext)
    {
        if (!scenarioContext.TryGetValue(out IServiceCollection services))
        {
            services = SetupKeyVault.GetServiceCollection();
            scenarioContext.Set(services);
        }

        return services;
    }

    internal static IConfiguration TryGetConfiguration(ScenarioContext scenarioContext)
    {
        if (!scenarioContext.TryGetValue(out IConfiguration configuration))
        {
            var services = TryGetServices(scenarioContext);
            configuration = services.AddConfiguration();
            services.AddMonitoring(configuration);
            scenarioContext.Set(configuration);
        }

        return configuration;
    }
}