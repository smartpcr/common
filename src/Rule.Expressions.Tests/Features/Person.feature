Feature: RuleEvaluator

  @rule
  @person
  Scenario: can evaluate simple property path
    Given a person
      | FirstName | LastName | Age |
      | John      | Doe      | 36  |
    And his wife
      | FirstName | LastName | Age |
      | Jen       | Bush     | 30  |
    And their children
      | FirstName | LastName | Age |
      | Julia     | Doe      | 18  |
      | Claire    | Bush     | 8   |
    When evaluate int value with following property path "Age"
    Then the result should be 36
    And person should be
      | FirstName | LastName | Age |
      | John      | Doe      | 36  |

  @rule
  @person
  Scenario: can evaluate compound property path
    Given a person
      | FirstName | LastName | Age |
      | John      | Doe      | 36  |
    And his wife
      | FirstName | LastName | Age |
      | Jen       | Bush     | 30  |
    And their children
      | FirstName | LastName | Age |
      | Julia     | Doe      | 18  |
      | Claire    | Bush     | 8   |
    When evaluate int value with following property path "Spouse.Age"
    Then the result should be 30

  @rule
  @person
  Scenario: can evaluate array index property path
    Given a person
      | FirstName | LastName | Age |
      | John      | Doe      | 36  |
    And his wife
      | FirstName | LastName | Age |
      | Jen       | Bush     | 30  |
    And their children
      | FirstName | LastName | Age |
      | Julia     | Doe      | 18  |
      | Claire    | Bush     | 8   |
    When evaluate int value with following property path "Children[0].Age"
    Then the result should be 18