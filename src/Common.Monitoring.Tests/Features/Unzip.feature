Feature: Unzip
As a user,
I want to be able to extract zip files,
So that I can put all etl files in one folder.

  @unzip
  Scenario: Extract zip files
    Given Given one or more zip files in folder "C:\\zips"
    When I extract zip files to collect etl files to folder "C:\\etls"
    Then I should see all etl files in folder "C:\\etls"

  @unzip
  Scenario: Extract hci logs
    Given A zip file at "%HOME%\\Downloads\\hci.zip"
    When I extract "etl" files from zip file to folder "%HOME%\\Downloads\\hci\\etw"
    Then I should see the following "etl" files in folder "%HOME%\\Downloads\\hci\\etw"
      | FileName                                                             |
      | V-HOST1_AzureStack.Common.Infrastructure.2024-10-09.1.etl            |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.1.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.2.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.3.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.4.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.5.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.6.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.7.etl |
      | V-HOST1_AzureStack.Common.Infrastructure.Middleware.2024-10-09.8.etl |
      | V-HOST1_AzureStack.ECE.2024-10-09.1.etl                              |
      | V-HOST1_AzureStack.ECE.2024-10-09.2.etl                              |
      | V-HOST1_AzureStack.ECE.2024-10-09.3.etl                              |
      | V-HOST1_AzureStack.ECEAgentCommonInfra.2024-10-09.1.etl              |
      | V-HOST1_AzureStack.Roles.VirtualMachines.2024-10-09.1.etl            |
      | V-HOST1_AzureStack.Roles.VirtualMachines.2024-10-09.2.etl            |
      | V-HOST1_AzureStack.Update.Admin.2024-10-09.1.etl                     |
      | v-Host1_AzureStackAgentLifecycleAgent.etl                            |
      | v-Host1_lcmControllerLogmanTraces.etl                                |
    When I extract "evtx" files from zip file to folder "%HOME%\\Downloads\\hci\\evtx"
    Then I should see the following "evtx" files in folder "%HOME%\\Downloads\\hci\\evtx"
      | FileName                                                        |
      | Event_Microsoft.AzureStack.LCMController.EventSource-Admin.evtx |
      | Event_Microsoft-Windows-WinRM-Operational.EVTX                  |
      | Event_Microsoft-Windows-WMI-Activity-Operational.EVTX           |