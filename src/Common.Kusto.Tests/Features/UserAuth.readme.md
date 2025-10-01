# User Authentication Test for Kusto

This feature tests user authentication to Azure Data Explorer (Kusto) using interactive user credentials.

## Purpose

Demonstrates how to:
- Connect to a production Kusto cluster using `WithAadUserPromptAuthentication()`
- Execute KQL queries against the ICM Data Warehouse
- Process query results with strongly-typed objects
- Handle user authentication prompts during test execution

## Test Details

### Cluster Information
- **Cluster**: `icmcluster.kusto.windows.net`
- **Database**: `IcmDataWarehouse`
- **Authentication**: Azure AD User Prompt (interactive)

### Query
The test queries the `Incidents` table for recent incidents (last 10 days) from specific services:
- AZURESTACKHCI
- MSAKS
- ARCAPPLIANCE

Results are grouped by ServiceName and TeamName with counts and earliest creation date.

## Running the Test

### Prerequisites
1. Access to the ICM cluster (requires Azure AD permissions)
2. .NET 8.0 SDK installed
3. Azure CLI or Visual Studio for authentication (optional, but helpful for pre-auth)

### Execution

#### Run all tests in the feature
```bash
cd src/Common.Kusto.Tests
dotnet test --filter "FullyQualifiedName~UserAuthFeature"
```

#### Run specific scenario
```bash
dotnet test --filter "FullyQualifiedName~QueryIcmClusterWithUserAuthentication"
```

#### Run with verbose output
```bash
dotnet test --filter "FullyQualifiedName~UserAuthFeature" --logger "console;verbosity=detailed"
```

### Expected Behavior

1. **Authentication Prompt**: You will see an Azure AD authentication prompt
   - Browser-based: A browser window will open for sign-in
   - Device code: You'll receive a code to enter at https://microsoft.com/devicelogin

2. **Query Execution**: After successful authentication, the query executes

3. **Results**: Test output shows:
   ```
   Service: AZURESTACKHCI, Team: HCICore, Count: 15, First Created: 2024-01-15
   Service: MSAKS, Team: AKSRuntime, Count: 8, First Created: 2024-01-16
   ...
   ```

## Manual Testing Tag

This test is tagged with `@manual_test` because:
- It requires interactive user authentication
- It queries a production cluster
- Results depend on real-time incident data

To exclude from automated CI/CD:
```bash
# Run all tests EXCEPT manual tests
dotnet test --filter "Category!=manual_test"
```

## Direct Code Usage

You can also use this pattern directly in your code:

```csharp
using Kusto.Data;
using Kusto.Data.Net.Client;

var cluster = "https://icmcluster.kusto.windows.net";
var database = "IcmDataWarehouse";

// Create connection string with user prompt authentication
var kcsb = new KustoConnectionStringBuilder(cluster)
    .WithAadUserPromptAuthentication();

// Create query client
var queryClient = KustoClientFactory.CreateCslQueryProvider(kcsb);
queryClient.DefaultDatabaseName = database;

// Execute query
var query = "Incidents | where CreateDate > ago(1d) | take 10";
var reader = await queryClient.ExecuteQueryAsync(database, query, null);

while (reader.Read())
{
    // Process results
}
```

## Troubleshooting

### Authentication Fails
- Ensure you have access to the ICM cluster
- Check that your Azure AD account has proper permissions
- Try clearing cached credentials: `az account clear`

### No Results Returned
- The query filters for specific services and last 10 days
- Verify incidents exist in that timeframe
- Try removing service filters to see all incidents

### Timeout Errors
- ICM cluster might be busy
- Increase timeout in ClientRequestProperties:
  ```csharp
  var props = new ClientRequestProperties
  {
      ServerTimeout = TimeSpan.FromMinutes(5)
  };
  ```

## Related Documentation

- [Kusto Authentication Modes](../readme.md#authentication-architecture)
- [Azure Data Explorer User Authentication](https://docs.microsoft.com/azure/data-explorer/kusto/api/connection-strings/kusto#user-authentication)
- [ICM Data Warehouse Schema](https://icmdocs.azurewebsites.net/developers/dataWarehouse.html)
