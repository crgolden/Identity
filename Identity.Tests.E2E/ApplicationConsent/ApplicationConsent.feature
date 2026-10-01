@E2E
Feature: Consent
  When an application asks for access, the member decides which permissions it gets.

  Scenario: A member approves an application's request and the application receives authorization
    Given a signed-in member
    And an application that asks for consent
    When the application asks the member for access
    And they allow every requested permission
    Then the application receives an authorization code

  Scenario: A member declines an application's request and the application is told access was denied
    Given a signed-in member
    And an application that asks for consent
    When the application asks the member for access
    And they decline the request
    Then the application is told access was denied

  Scenario: A member who allows nothing is asked to choose at least one permission
    Given a signed-in member
    And an application that asks for consent
    When the application asks the member for access
    And they allow none of the requested permissions
    Then they are asked to choose at least one permission
