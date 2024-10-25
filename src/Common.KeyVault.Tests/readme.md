## Manual setup before running tests

1. register aad app
2. create a keyvault
3. grant secret list permission to aad app
4. setup app client secret, store in keyvault secret
5. setup app client certificate, store in keyvault cert and secret

### create aad app cert with c#

```csharp
var certificate = new X509Certificate2(@"C:\\path\\to\\certificate.pfx", "password");
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create("<client_id>")
    .WithCertificate(certificate)
    .WithAuthority(new Uri("https://login.microsoftonline.com/<tenant_id>"))
    .Build();

string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

Console.WriteLine($"Access Token: {result.AccessToken}");

```

### create aad app cert with openssl

```bash
openssl genpkey -algorithm RSA -out longhorn17-status-report-api.key
openssl req -new -key longhorn17-status-report-api.key -out longhorn17-status-report-api.csr -subj "/CN=longhorn17-status-report-api"
openssl x509 -req -days 365 -in longhorn17-status-report-api.csr -signkey longhorn17-status-report-api.key -out longhorn17-status-report-api.crt

az login --use-device-code --tenant 625a8c92-2669-4d71-8ac3-923a55242192
az account set --subscription "c5a015e6-a59b-45bd-a621-82f447f46034"

az ad app credential reset --id 44b5af6b-2720-494d-b53c-ffbb631d50c1 --cert "@./longhorn17-status-report-api.crt"

cat longhorn17-status-report-api.key longhorn17-status-report-api.crt > longhorn17-status-report-api.pem
az keyvault secret set --vault-name "akshci-kv-xiaodong" --name "longhorn17-status-report-api-cert" --file "longhorn17-status-report-api.pem"

```


## Update appsettings.json

1. aad auth: tenantId, clientId, clientSecret, clientCertificateName
2. keyvault: vaultName