@unit_test
Feature: Operator Evaluation
  In order to evaluate rules
  As a developer
  I want to be able to compile rule expression against a strongly typed object

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

  Scenario: Verify context with nested properties returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
      """
      {
        "allOf": [
          {
            "left": "FirstName",
            "operator": "Equals",
            "right": "Donald"
          },
          {
            "left": "LastName",
            "operator": "Equals",
            "right": "Trump"
          },
          {
            "left": "Spouse.FirstName",
            "operator": "Equals",
            "right": "Melania"
          }
        ]
      }
      """
    Then evaluation result should be "true"

  Scenario: Verify context with array contains filter returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "FirstName",
          "operator": "Equals",
          "right": "Donald"
        },
        {
          "left": "Hobbies",
          "operator": "Contains",
          "right": "Golf"
        },
        {
          "left": "Spouse.FirstName",
          "operator": "NotEquals",
          "right": "Ivanka"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with indexed array filter returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "FirstName",
          "operator": "Equals",
          "right": "Donald"
        },
        {
          "left": "Hobbies",
          "operator": "Contains",
          "right": "Golf"
        },
        {
          "left": "Spouse.FirstName",
          "operator": "StartsWith",
          "right": "Mel"
        },
        {
          "left": "Children[0].FirstName",
          "operator": "Equals",
          "right": "Tiffany"
        },
        {
          "left": "Titles[1]",
          "operator": "NotEquals",
          "right": "Scientist"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with enum field in filter returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "FirstName",
          "operator": "Equals",
          "right": "Donald"
        },
        {
          "left": "Race",
          "operator": "In",
          "right": "Black,White"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with date field filter returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "FirstName",
          "operator": "Equals",
          "right": "Donald"
        },
        {
          "left": "BirthDate",
          "operator": "LessThan",
          "right": "2020-04-07T15:47:54.760654-07:00"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with expression on each side returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children[0].LastName",
          "operator": "Equals",
          "right": "Children[1].LastName",
          "rightSideIsExpression": true
        },
        {
          "left": "Children.Count()",
          "operator": "Equals",
          "right": "2"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with match operator returns true
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "FirstName",
          "operator": "Matches",
          "right": "^do[\\w]+d$"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: Verify context with match operator returns false
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "LastName",
          "operator": "Matches",
          "right": "^DO[\\w]+d$"
        }
      ]
    }
    """
    Then evaluation result should be "false"