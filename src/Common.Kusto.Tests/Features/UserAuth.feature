Feature: UserAuth
As a developer,
I want to authenticate to Kusto using user credentials,
So that I can query production clusters interactively.

  @manual_test
  Scenario: Query ICM cluster with user authentication
    Given I connect to ICM cluster with user authentication
    When I execute query for recent incidents
    Then I should receive incident summary data
    And the results should contain service names
