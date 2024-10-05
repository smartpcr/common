Feature: BatchIngest
As a user,
I want to be able to ingest json files in batch to kusto database,
So that I can query them later.

    Scenario: batch ingest json files
        Given json file folder "TestData"
        When Ensure kusto table "People" exists
        When I ingest json files
        Then kusto db should have table "People"
        And table "People" should have 10 rows