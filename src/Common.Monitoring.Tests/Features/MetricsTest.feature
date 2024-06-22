@integration_test
Feature: MetricsTest
	Should be able to create counter, histogram and gauge metrics

    @prod @counter
    Scenario: Create a counter for total requests
	    Given a web api is running on port 19001
        And monitoring settings are configured with metrics capability
	    When I call api endpoint "/api/hello" 3 times
	    Then the metric "total_requests" should be 3