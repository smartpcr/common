@integration_test
Feature: MetricsTest
  As a developer,
  I want to be able to create counter, histogram and gauge metrics,
  So that I can monitor the system.

  @prod
  @counter
  Scenario: Create a counter for total requests
    Given monitoring settings are configured with metrics capability
    And setup api handler for request "/health" to return "I'm healthy"
    When I call api endpoint "/health" 3 times
    Then the metric "total_requests" should be 3