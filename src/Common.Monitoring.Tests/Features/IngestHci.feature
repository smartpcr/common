Feature: ingest HCI etw and evtx events
  Background:
    Given kusto cluster uri "http://192.168.23.191:8080"
    And kusto database name "hci"
    And kustainer volume mount from "E:\\kustodata" to "/kustodata"

  Scenario: end to end ingestion of HCI logs
    Given A zip file at "%HOME%\\Downloads\\hci.zip"
    When I extract "etl" files from zip file to folder "%HOME%\\Downloads\\hci\\etw"
    Then I should see the following "etl" files in folder "%HOME%\\Downloads\\hci\\etw"
      | FileName                                         |
      | V-HOST1_AzureStack.Update.Admin.2024-10-09.1.etl |
    When I extract "evtx" files from zip file to folder "%HOME%\\Downloads\\hci\\evtx"
    Then I should see the following "evtx" files in folder "%HOME%\\Downloads\\hci\\evtx"
      | FileName                                                        |
      | Event_Microsoft.AzureStack.LCMController.EventSource-Admin.evtx |
    When I parse etl files in folder "%HOME%\\Downloads\\hci\\etw"
    Then I should find 121 distinct events in etl files
    When I create tables based on etl event schemas
    Then I should see following etl kusto tables
    | TableName                                                                          |
    | ETL-Microsoft-URP-InfraEventSource.HealthCheckResultDirectoryIsEmptyOrDoesNotExist |
    | ETL-Microsoft-URP-InfraEventSource.ResolverGetAll                                  |
    | ETL-Microsoft-URP-InfraEventSource.StopService                                     |
    When I extract etl files in folder "%HOME%\\Downloads\\hci\\etw" to csv files in folder "%HOME%\\Downloads\\hci\\csv"
    Then I should see following csv files in folder "%HOME%\\Downloads\\hci\\csv"
    | FileName                                              |
    | ETL-Microsoft-URP-InfraEventSource.ResolverGetAll.csv |
    | ETL-Microsoft-URP-InfraEventSource.StartService.csv   |
    | ETL-Microsoft-URP-InfraEventSource.StopService.csv    |
    When I parse evtx files in folder "%HOME%\\Downloads\\hci\\evtx"
    Then I should find 4475 distinct records in evtx files
    When I create table based on evtx record schema
    Then I should see following evtx kusto table
      | TableName     |
      | WindowsEvents |
    When I extract evtx records to csv files in folder "%HOME%\\Downloads\\hci\\csv"
    Then I should see following csv file "WindowsEvents.csv" in folder "%HOME%\\Downloads\\hci\\csv"
    When I ingest csv files in folder "%HOME%\\Downloads\\hci\\csv" to kusto
    Then the following kusto tables should have added records with expected counts
      | TableName | RecordCount |
      | WindowsEvents | 4475 |
