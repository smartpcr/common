Feature: EtwIngest
	As a user,
  I want to be able to extract ETW events from a file,
  and infer its kusto table schema based on provider and event,
  and ingest the events into the kusto table.

@etw
Scenario: extract etl file
	Given etl file "E:\\kustodata\\SAC14-S1-N01_Microsoft-AzureStack-Compute-HostPluginWatchDog.2024-09-23.1.etl"
	When I parse etl file
	Then the result have the following events
	  | ProviderName                                    | EventName                  |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | ConfigFileFound            |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | EnsureProcessStarted/Start |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | EnsureProcessStarted/Stop  |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | FoundProcessAlreadyRunning |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | ProcessStarted             |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | ReadConfigFromStore        |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | StartWatchDog/Start        |
    | Microsoft-AzureStack-Compute-HostPluginWatchDog | StartWatchDog/Stop         |
