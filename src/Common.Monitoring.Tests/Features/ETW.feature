Feature: ETW
	As a developer,
  I want to be able to write ETW event without specifying provider and event name,
  So that I can integrate it with tracing and logging.

@etw
Scenario: Add two numbers
	Given the first number is 50
	And the second number is 70
	When the two numbers are added
	Then the result should be 120