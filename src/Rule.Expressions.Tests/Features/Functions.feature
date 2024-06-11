Feature: Function Evaluation
  In order to use function in rule expression
  As a developer
  I want to be able to invoke built-in functions against a strongly typed object

Scenario: Check enum field is null
  Given a context of type "Person" from json file "donald_trump.json"
  When I evaluate context with JSON filter
  """
  {
    "allOf": [
      {
        "left": "Gender",
        "operator": "IsNull"
      }
    ]
  }
  """
  Then evaluation result should be "true"

Scenario: Check enum field is not null
  Given a context of type "Person" from json file "donald_trump.json"
  When I evaluate context with JSON filter
  """
  {
    "allOf": [
      {
        "left": "Gender",
        "operator": "NotIsNull",
      }
    ]
  }
  """
  Then evaluation result should be "false"

  Scenario: Check date field is not null
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "BirthDate",
          "operator": "NotIsNull"
        },
        {
          "left": "BirthDate",
          "operator": "GreaterThan",
          "right": "11/22/1943 10:52:28 PM"
        }
      ]
    }
    """
    Then evaluation result should be "true"

Scenario: Check date field is null
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Spouse.BirthDate",
          "operator": "NotIsNull"
        }
      ]
    }
    """
    Then evaluation result should be "false"

  Scenario: get count of object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Count()",
          "operator": "Equals",
          "right": "2"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get count of string array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Titles.Count()",
          "operator": "Equals",
          "right": "2"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get count of string list
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Hobbies.Count()",
          "operator": "Equals",
          "right": "2"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get distinct count of string list
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Titles.DistinctCount()",
          "operator": "Equals",
          "right": "2"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: select from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(FirstName)",
          "operator": "AllIn",
          "right": "Tiffany, Barron"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: select many from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.SelectMany(Hobbies)",
          "operator": "AllIn",
          "right": "Arts, Music, Dancing, Soccer"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: summarize from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Age).Sum()",
          "operator": "Equals",
          "right": "40"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: summarize field from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Sum(Age)",
          "operator": "Equals",
          "right": "40"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get average from array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Age).Average()",
          "operator": "Equals",
          "right": "20"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get average with arg from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Average(Age)",
          "operator": "Equals",
          "right": "20"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get max from array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Age).Max()",
          "operator": "Equals",
          "right": "26"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get max with arg from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Max(Age)",
          "operator": "Equals",
          "right": "26"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get min from array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Age).Min()",
          "operator": "Equals",
          "right": "14"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: get min with arg from object array
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Min(Age)",
          "operator": "Equals",
          "right": "14"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: use expression on both sides
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Age).Sum()",
          "operator": "LessThan",
          "right": "Age",
          "rightSideIsExpression": true
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: select nested field
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(FirstName)",
          "operator": "AllIn",
          "right": "Tiffany,Barron"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: select nested field with function
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Select(Hobbies.OrderByDesc().First())",
          "operator": "AllIn",
          "right": "Music,Soccer"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function ago
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "BirthDate",
          "operator": "GreaterThan",
          "right": "Ago(50000d)",
          "rightSideIsExpression": true
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function where
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Where(FirstName, Equals, Tiffany).Count()",
          "operator": "Equals",
          "right": "1"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function where with contains
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Where(LastName, contains, 'ump').Select(FirstName)",
          "operator": "AllIn",
          "right": "Tiffany,Barron"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function first with arg
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.First(FirstName, Equals, Tiffany).Age",
          "operator": "GreaterThan",
          "right": "25"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function first without arg
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Hobbies.First()",
          "operator": "Equals",
          "right": "Golf"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function last with arg
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Last(FirstName, Equals, Tiffany).Age",
          "operator": "GreaterThan",
          "right": "14"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function last without arg
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Hobbies.Last()",
          "operator": "In",
          "right": "Golf,Tweeter"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function order by
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.OrderBy(FirstName).First().FirstName",
          "operator": "Equals",
          "right": "Barron"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke function order by desc
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.OrderByDesc(FirstName).First().FirstName",
          "operator": "Equals",
          "right": "Tiffany"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke macro
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "IsPresident()",
          "operator": "Equals",
          "right": "true"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke macro with args
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "IsAdult(18)",
          "operator": "Equals",
          "right": "true"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke sum on empty collection
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Where(Age,lessThan,0).Select(Age).Sum()",
          "operator": "Equals",
          "right": "0"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke max on empty collection
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Where(FirstName,equals,helloworld).Max(Age)",
          "operator": "Equals",
          "right": "0"
        }
      ]
    }
    """
    Then evaluation result should be "true"

  Scenario: invoke count on empty collection
    Given a context of type "Person" from json file "donald_trump.json"
    When I evaluate context with JSON filter
    """
    {
      "allOf": [
        {
          "left": "Children.Where(Age,lessThan,0).Count()",
          "operator": "Equals",
          "right": "0"
        }
      ]
    }
    """
    Then evaluation result should be "true"