namespace Common.KeyVault.Tests.Hooks;

using Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monitoring;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;


[Binding]
public sealed class SetupKeyVault
{
    private readonly ScenarioContext context;
    private readonly ISpecFlowOutputHelper outputHelper;

    public SetupKeyVault(ScenarioContext scenarioContext, ISpecFlowOutputHelper outputHelper)
    {
        this.context = scenarioContext;
        this.outputHelper = outputHelper;
    }

    [BeforeScenario("User")]
    public void SetupUserAuth()
    {
        SetupVautAuthType(VaultAuthType.User);
    }

    [BeforeScenario("Msi")]
    public void SetupMsiAuth()
    {
        SetupVautAuthType(VaultAuthType.Msi);
    }

    [BeforeScenario("ClientSecret")]
    public void SetupSpnWithClientSecretsAuth()
    {
        SetupVautAuthType(VaultAuthType.SpnWithSecretOnFile);
    }

    [BeforeScenario("ClientCertificate")]
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
            services = new ServiceCollection();
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