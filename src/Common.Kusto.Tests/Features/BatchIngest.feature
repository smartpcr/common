Feature: Batch ingest
  Scenario: batch ingest json files to kusto
    Given json file folder "testdata\\json"
    When I ingest json files
    Then kusto db should have table "Places"
    And table "Places" should have 10 rows