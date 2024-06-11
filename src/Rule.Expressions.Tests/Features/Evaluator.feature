Feature: Evaluation
  In order to rules
  As a developer
  I want to be able to evaluate rule expression against a strongly typed object

  Scenario: Verify context with equal filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
    | Field | Operator | Value   |
    | City  | Equals    | Redmond |
    Then evaluation result should be "true"

  Scenario: Verify context with number filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field      | Operator    | Value  |
      | Population | GreaterThan | 100000 |
    Then evaluation result should be "true"

  Scenario: Verify context with number filter returns false
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field     | Operator    | Value  |
      | AvgIncome | LessOrEqual | 100000 |
    Then evaluation result should be "false"

  Scenario: Verify context with in filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field | Operator | Value    |
      | State | In       | CA,AZ,WA |
    Then evaluation result should be "true"

  Scenario: Verify context with in filter trim white spaces returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field | Operator | Value            |
      | State | In       | CA, AZ, WA, , DC |
    Then evaluation result should be "true"

  Scenario: Verify context with in filter returns false
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field | Operator | Value  |
      | State | In       | CA, AZ |
    Then evaluation result should be "false"

  Scenario: Verify context with allin filter returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate the rule expression
      | Field   | Operator | Value         |
      | Hobbies | AllIn    | Golf, Tweeter |
    Then evaluation result should be "true"

  Scenario: Verify context with null field filter returns false
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field  | Operator | Value |
      | Street | Contains | Main  |
    Then evaluation result should be "false"

  Scenario: Verify context with contains filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field | Operator | Value |
      | City  | Contains | mond  |
    Then evaluation result should be "true"

  Scenario: Verify context with startswith filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate the rule expression
      | Field | Operator | Value |
      | City  | Contains | Red   |
    Then evaluation result should be "true"

  Scenario: Verify context with composite filter returns true
    Given a context of type "Location" from json file "redmond.json"
    When I evaluate context with JSON filter
      """
      {
        "allOf": [
          {
            "not": {
              "left": "Country",
              "operator": "equals",
              "right": "Canada"
            }
          },
          {
            "left": "City",
            "operator": "equals",
            "right": "Redmond"
          },
          {
            "left": "State",
            "operator": "equals",
            "right": "WA"
          }
        ]
      }
      """
    Then evaluation result should be "true"

  Scenario: Verify context with nested filter returns true
    Given an array of type "Person" from json file "big_family.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "LastName",
          "operator": "Equals",
          "right": "Smith"
        },
        {
          "not": {
            "anyOf": [
              {
                "allOf": [
                  {
                    "left": "FirstName",
                    "operator": "In",
                    "right": "Mary"
                  },
                  {
                    "left": "LastName",
                    "operator": "Equals",
                    "right": "Smith"
                  }
                ]
              },
              {
                "allOf": [
                  {
                    "left": "FirstName",
                    "operator": "Equals",
                    "right": "Patricia"
                  },
                  {
                    "left": "LastName",
                    "operator": "Equals",
                    "right": "Smith"
                  }
                ]
              },
              {
                "anyOf": [
                  {
                    "left": "FirstName",
                    "operator": "Equals",
                    "right": "Linda"
                  },
                  {
                    "left": "FirstName",
                    "operator": "Equals",
                    "right": "Jennifer"
                  },
                  {
                    "left": "FirstName",
                    "operator": "Equals",
                    "right": "Margaret"
                  }
                ]
              }
            ]
          }
        }
      ]
    }
    """
    Then collection evaluation result should be
    | FirstName |
    | James     |
    | John      |
    | Robert    |
    | William   |
    | David     |