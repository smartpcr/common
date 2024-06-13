Feature: keyvault
	Simple calculator for adding two numbers

    @integration_test @User
    Scenario: List all secrets from keyvault using user auth type
	    Given vault auth type user
	    When I list all secrets
	    Then I should get list of secret names

    @integration_test @Msi
    Scenario: List all secrets from keyvault using msi auth type
        Given vault auth type spn
        When I list all secrets
        Then I should get list of secret names

    # requires application permission to keyvault
    @integration_test @ClientSecret @ignore
    Scenario: List all secrets from keyvault using client secret auth type
        Given vault auth type client secret with file "longhorn17-status-report-api-pwd"
        When I list all secrets
        Then I should get list of secret names

    # requires application permission to keyvault
    @integration_test @ClientCertificate @ignore
    Scenario: List all secrets from keyvault using client certificate auth type
        Given vault auth type client secret with certificate "longhorn17-status-report-api-cert.pem"
        When I list all secrets
        Then I should get list of secret names