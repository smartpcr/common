Feature: ETW
As a developer,
I want to be able to write ETW event without specifying provider and event name,
So that I can integrate it with tracing and logging.

  @etw
  @prod
  Scenario: Generate ETW event with provider and event name
    Given the system have 4 services with the following methods and events
      | ServiceName | MethodName    | EventId | EventName               | Tags                                                   | Latency | ChildCall           |
      | FrontEnd    | HandleRequest | 1       | RequestReceived         | {"path":"/products"}                                   | 1       |                     |
      | FrontEnd    | HandleRequest | 2       | GetProducts             | {"path":"/products"}                                   | 4       | BackEnd.GetProducts |
      | BackEnd     | GetProducts   | 3       | BeginGetProducts        |                                                        | 10      |                     |
      | BackEnd     | GetProducts   | 4       | GetProductsFromCache    | {"cache_key": "/products"}                             | 0       | Cache.TryGet        |
      | Cache       | TryGet        | 5       | CacheMiss               | {"cache_key": "/products", "found": false}             | 5       |                     |
      | BackEnd     | GetProducts   | 6       | GetProductsFromDatabase | {"query": "SELECT * FROM products"}                    | 0       | Database.Query      |
      | Database    | Query         | 7       | BeginQuery              | {"query": "SELECT * FROM products"}                    | 50      |                     |
      | Database    | Query         | 8       | FinishedQuery           | {"count": 15}                                          | 300     |                     |
      | BackEnd     | GetProducts   | 9       | UpdateCache             | {"cache_key": "/products", "count": 15}                | 20      |                     |
      | BackEnd     | GetProducts   | 10      | GetProductsStop         | {"count": 15}                                          | 10      |                     |
      | FrontEnd    | HandleRequest | 11      | RequestCompleted        | {"path":"/products", "count": 15, "status_code": "OK"} | 20      |                     |
    When I call "HandleRequest" method of "FrontEnd" service to get list of products
    Then the system should have collected the following events before timeout of 10 seconds
      | Order | ProviderName | EventName                       | Tags                                                   | Parent              |
      | 1     | FrontEnd     | RequestReceived                 | {"path":"/products"}                                   |                     |
      | 2     | FrontEnd     | GetProducts                     | {"path":"/products"}                                   |                     |
      | 3     | BackEnd      | GetProducts                     |                                                        |                     |
      | 4     | BackEnd      | BeginGetProductsFromCache       | {"cache_key": "/products"}                             | BackEnd.GetProducts |
      | 5     | BackEnd      | FinishedGetProductsFromCache    | {"cache_key": "/products", "found": false}             | BackEnd.GetProducts |
      | 6     | BackEnd      | BeginGetProductsFromDatabase    | {"query": "SELECT * FROM products"}                    | BackEnd.GetProducts |
      | 7     | Database     | BeginQuery                      | {"query": "SELECT * FROM products"}                    | Database.Query      |
      | 8     | Database     | FinishedQuery                   | {"count": 15}                                          | Database.Query      |
      | 9     | BackEnd      | FinishedGetProductsFromDatabase | {"count": 15}                                          | BackEnd.GetProducts |
      | 10    | BackEnd      | UpdateCache                     | {"cache_key": "/products"}                             | BackEnd.GetProducts |
      | 11    | BackEnd      | FinishedGetProducts             | {"count": 15}                                          | BackEnd.GetProducts |
      | 12    | FrontEnd     | RequestCompleted                | {"path":"/products", "count": 15, "status_code": "OK"} |                     |