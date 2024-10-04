Feature: batch ingest json files into Kusto

  Scenario: ingest json files into Kusto
    Given json files in folder "TestData\\JsonFiles"
    When I ingest json files into Kusto table "TestTable"
    Then the json files should be ingested into Kusto table "TestTable"