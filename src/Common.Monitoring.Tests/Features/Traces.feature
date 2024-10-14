Feature: TracesTest
As a developer,
I want to be able to correlate calls wihin a service, as well as across services,
So that I can monitor and troubleshoot the system.

  @trace
  @prod
  Scenario: Trace sync parent and child calls
    Given a number 4
    When I calculate fibonacci of the number
    Then the result should be 3
    And I should have the following traces
      | OperationName                      | ParentOperationName                | Attributes             |
      | GivenANumber                       |                                    | input.number: 4        |
      | Fibonacci                          |                                    | input.n: 0, result: 0  |
      | Fibonacci                          |                                    | input.n: 1, result: 1  |
      | Fibonacci                          | Fibonacci                          | input.n: 2, result 1   |
      | Fibonacci                          | Fibonacci                          | input.n: 3, result 2   |
      | Fibonacci                          | WhenICalculateFibonacciOfTheNumber | input.n: 4, result 3   |
      | WhenICalculateFibonacciOfTheNumber |                                    | result: 3              |
      | ThenTheResultShouldBe              |                                    | expected: 3, actual: 3 |