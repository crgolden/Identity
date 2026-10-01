@E2E
Feature: Managing providers
  An administrator registers the external identity providers members may sign in with, and the SAML service
  providers this server signs users into.

  Scenario Outline: An administrator registers a provider and lands on its details
    Given a signed-in administrator
    When they add a new <kind>
    Then they land on the new <kind>

    Examples:
      | kind                |
      | IdentityProvider    |
      | SamlServiceProvider |

  Scenario Outline: An administrator renames a provider and the new name is kept
    Given a signed-in administrator
    And a registered <kind>
    When they rename the <kind>
    Then the <kind> shows its new name

    Examples:
      | kind                |
      | IdentityProvider    |
      | SamlServiceProvider |

  Scenario Outline: An administrator deletes a provider and it leaves the list
    Given a signed-in administrator
    And a registered <kind>
    When they delete the <kind> from its details
    Then the <kind> is no longer registered

    Examples:
      | kind                |
      | IdentityProvider    |
      | SamlServiceProvider |
