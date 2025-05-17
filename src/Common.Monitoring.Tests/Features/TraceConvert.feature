Feature: TraceConvert
As a developer,
I want to be able to correlate calls wihin a service, as well as across services,
So that I can monitor and troubleshoot the system.


  @trace
  @prod
  Scenario: Export otel oltp trace to temp trace
    Given otlp trace file at "TestData/Traces/otlp-traces.json"
    When I export the trace to a tempo folder "TestData/Traces/tempo"
    Then the temp files should exist

  @trace
  @prod
  Scenario: Export large oltp trace to temp trace
    Given the following otlp trace files at "TestData/Traces"
    | FileName                            |
    | traces-2025-05-17T09-41-09.304.json |
    | traces-2025-05-17T12-27-08.769.json |
    | traces-2025-05-17T11-47-34.451.json |
    When I export all the traces to a tempo folder "TestData/Traces/tempo"
    Then the temp files should exist