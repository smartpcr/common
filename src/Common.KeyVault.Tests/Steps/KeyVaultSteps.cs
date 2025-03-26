// -----------------------------------------------------------------------
// <copyright file="KeyVaultSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault.Tests.Steps;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Config;
using FluentAssertions;
using Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monitoring;
using Reqnroll;

[Binding]
public class KeyVaultSteps
{
    private readonly ScenarioContext context;

    public KeyVaultSteps(ScenarioContext scenarioContext)
    {
        context = scenarioContext;
    }

    [Given(@"vault auth type user")]
    public void GivenVaultAuthTypeUser()
    {
        VerifyAuthType(VaultAuthType.User);
        SetupSecretProvider();
    }

    [Given(@"vault auth type spn")]
    public void GivenVaultAuthTypeSpn()
    {
        VerifyAuthType(VaultAuthType.Msi);
        SetupSecretProvider();
    }

    [Given(@"vault auth type client secret with file ""(.*)""")]
    public void GivenVaultAuthTypeClientSecretWithFile(string clientSecretFileName)
    {
        var vaultSettings = VerifyAuthType(VaultAuthType.SpnWithSecretOnFile);
        SetupVaultClientSecretFile(vaultSettings, clientSecretFileName);
        SetupSecretProvider();
    }

    [Given(@"vault auth type client secret with certificate ""(.*)""")]
    public void GivenVaultAuthTypeClientSecretWithCertificate(string clientCertificate)
    {
        var vaultSettings = VerifyAuthType(VaultAuthType.SpnWithCertOnFile);
        SetupVaultClientSecretFile(vaultSettings, clientCertificate);
        SetupSecretProvider();
    }

    [When(@"I list all secrets")]
    public async Task WhenIListAllSecrets()
    {
        var services = SetupKeyVault.TryGetServices(context);
        var serviceProvider = services.BuildServiceProvider();
        var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
        var secretNames = await secretProvider.ListSecretsAsync(CancellationToken.None);
        context.Set(secretNames.ToList(), "SecretNames");
    }

    [Then(@"I should get list of secret names")]
    public void ThenIShouldGetListOfSecretNames()
    {
        var secretNames = context.Get<List<string>>("SecretNames");
        secretNames.Should().NotBeNull();
        secretNames.Should().NotBeEmpty();
    }

    private VaultSettings VerifyAuthType(VaultAuthType authType)
    {
        var vaultSettings = context.Get<VaultSettings>();
        vaultSettings.Should().NotBeNull();
        vaultSettings!.AuthType.Should().Be(authType);

        var services = context.Get<IServiceCollection>();
        services.Should().NotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var vaultSettingsOptions = serviceProvider.GetService<IOptions<VaultSettings>>();
        vaultSettingsOptions.Should().NotBeNull();
        vaultSettingsOptions!.Value.AuthType.Should().Be(authType);
        vaultSettingsOptions.Value.VaultName.Should().NotBeNullOrEmpty();

        return vaultSettings;
    }

    private void SetupVaultClientSecretFile(VaultSettings vaultSettings, string clientSecretFileName)
    {
        if (vaultSettings.Aad == null)
        {
            var configuration = SetupKeyVault.TryGetConfiguration(context);
            var aadSettings = configuration.GetConfiguredSettings<AadSettings>();
            vaultSettings.Aad = new VaultAadSettings
            {
                SecretFileName = clientSecretFileName,
                TenantId = aadSettings.TenantId,
                ClientId = aadSettings.ClientId
            };
        }
        else
        {
            vaultSettings.Aad.SecretFileName = clientSecretFileName;
        }
    }

    private void SetupSecretProvider()
    {
        var services = SetupKeyVault.TryGetServices(context);
        var configuration = SetupKeyVault.TryGetConfiguration(context);
        services.ConfigureSettings<MonitorSettings>()
            .ConfigureSettings<AadSettings>()
            .AddOptions();
        services.AddKeyVault(configuration);
    }
}