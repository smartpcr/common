Feature: CircularBuffer

  @circular-buffer
  Scenario: circular buffer should not exceed its capacity
    Given capacity of 100
    When add 110 message
    Then buffer count should be 100
    And dropped message count should be 10

  @circular-buffer
  Scenario: circular buffer keeps the most recent message and drops most old message when producer rate is higher
    Given capacity of 100
    When perform the following iterations
      | Step | ProducerRate | ConsumerRate | Message |
      | 1    | 50           | 30           | one     |
      | 2    | 50           | 30           | two     |
      | 3    | 50           | 30           | three   |
      | 4    | 50           | 30           | four    |
      | 5    | 50           | 30           | five    |
      | 6    | 50           | 30           | six     |
      | 7    | 50           | 30           | seven   |
      | 8    | 50           | 30           | eight   |
      | 9    | 50           | 30           | nine    |
      | 10   | 50           | 30           | ten     |
    Then buffer count should be 100
    And the messages in buffer should be
      | Message | Count |
      | ten     | 50    |
      | nine    | 50    |