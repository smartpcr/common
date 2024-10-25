@integration_test
Feature: keyvault
	as a user,
  I want to be able to access secret from keyvault, using different auth types
  So that I can use the secret in my application

    @User
    Scenario: List all secrets from keyvault using user auth type
	    Given vault auth type user
	    When I list all secrets
	    Then I should get list of secret names

    @Msi
    Scenario: List all secrets from keyvault using msi auth type
        Given vault auth type spn
        When I list all secrets
        Then I should get list of secret names

    # requires application permission to keyvault
    @ClientSecret
    Scenario: List all secrets from keyvault using client secret auth type
        Given vault auth type client secret with file "longhorn17-status-report-api-pwd"
        When I list all secrets
        Then I should get list of secret names

    # requires application permission to keyvault
    @ClientCertificate
    Scenario: List all secrets from keyvault using client certificate auth type
        Given vault auth type client secret with certificate "longhorn17-status-report-api-cert.pem"
        When I list all secrets
        Then I should get list of secret names