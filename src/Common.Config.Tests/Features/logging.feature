Feature: logging
	Simple calculator for adding two numbers

    @unit_test @logging @prod
    Scenario: console logger
	    Given logger is configured
        When I create several logs
          | Level       | Message                                                                                                                     |
          | Debug       | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. |
          | Information | Pharetra pharetra massa massa ultricies mi quis hendrerit.                                                                  |
          | Warning     | Fusce ut placerat orci nulla. Ac ut consequat semper viverra nam.                                                           |
          | Error       | Placerat orci nulla pellentesque dignissim enim sit amet venenatis urna.                                                    |
        Then the following messages should be logged
          | Level       | Message                                                                                                                     |
          | Debug       | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. |
          | Information | Pharetra pharetra massa massa ultricies mi quis hendrerit.                                                                  |
          | Warning     | Fusce ut placerat orci nulla. Ac ut consequat semper viverra nam.                                                           |
          | Error       | Placerat orci nulla pellentesque dignissim enim sit amet venenatis urna.                                                    |