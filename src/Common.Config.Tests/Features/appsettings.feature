Feature: appsettings

    @unit_test @prod
    Scenario: Get appsettings in production environment
        Given I have a valid "appsettings.json" file
        When I get the appsettings
        Then I should get the following values
            | Key                      | Value |
            | Logging:LogLevel:Default | Information |

    @unit_test @dev @over_ride
    Scenario: Get appsettings in Development environment
        Given I have a valid "appsettings.Development.json" file
        When I get the appsettings
        Then I should get the following values
          | Key                      | Value |
          | Logging:LogLevel:Default | Debug |

    @unit_test @strong_type @default
    Scenario: Get strong typed config setting
        Given setting file containing "KeyVaultSettings"
        When I get the appsettings
        Then I should get key vault
            | VaultName | AuthMode |
            | dummy     | Spn      |

    @unit_test @dev @strong_type @over_ride
    Scenario: Get strong typed config setting in dev environment
        Given setting file containing "KeyVaultSettings"
        When I get the appsettings
        Then I should get key vault
          | VaultName | AuthMode |
          | dummy     | Msi      |

    @unit_test @prod @strong_type @validation
    Scenario: validation successful
        Given setting file containing "Connection"
        When I get the appsettings
        Then I should get connection
        And I should NOT get any validation errors

    @unit_test @dev @strong_type @validation
    Scenario: validation failed in dev environment
        Given setting file containing "Connection"
        When I get the appsettings
        Then I should get connection
        And I should get validation errors
            | PropertyName | ErrorMessage                                |
            | Name         | The Name field is required.                 |
            | Port         | The field Port must be between 1 and 65535. |
