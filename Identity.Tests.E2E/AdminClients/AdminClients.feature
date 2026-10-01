@E2E
Feature: Managing clients
  An administrator registers the applications that may sign users in, and maintains each one's settings and lists.

  Scenario: An administrator opens the client list and can register a new client
    Given a signed-in administrator
    When they open the client list
    Then the client list is shown with a way to register a client

  Scenario: An administrator registers a new client and lands on its details
    Given a signed-in administrator
    When they register a new client
    Then they see the new client's details

  Scenario: A client's details offer editing and deleting
    Given a signed-in administrator
    And a client
    When they open the client's details
    Then they can edit or delete the client

  Scenario: An administrator opens a client's settings for editing
    Given a signed-in administrator
    And a client
    When they open the client's settings for editing
    Then the settings form is ready to save

  Scenario: An administrator changes a client setting and it is kept
    Given a signed-in administrator
    And a client
    When they change a client setting and save
    Then the changed setting is kept

  Scenario: An administrator deletes a client and it leaves the list
    Given a signed-in administrator
    And a client
    When they delete the client from the client list
    Then the client is no longer listed

  Scenario Outline: An administrator adds an entry to a client's list
    Given a signed-in administrator
    And a client
    When they add an entry to its <collection>
    Then its <collection> show the new entry

    Examples:
      | collection             |
      | Claims                 |
      | CorsOrigins            |
      | GrantTypes             |
      | IdPRestrictions        |
      | PostLogoutRedirectUris |
      | Properties             |
      | RedirectUris           |
      | Scopes                 |
      | Secrets                |

  Scenario Outline: An administrator removes an entry from a client's list
    Given a signed-in administrator
    And a client
    And its <collection> hold an entry
    When they remove that entry
    Then its <collection> no longer show the entry

    Examples:
      | collection             |
      | Claims                 |
      | CorsOrigins            |
      | GrantTypes             |
      | IdPRestrictions        |
      | PostLogoutRedirectUris |
      | Properties             |
      | RedirectUris           |
      | Scopes                 |
      | Secrets                |

  Scenario Outline: An administrator changes an entry in a client's list
    Given a signed-in administrator
    And a client
    And its <collection> hold an entry
    When they change that entry
    Then its <collection> show the changed entry

    Examples:
      | collection             |
      | Claims                 |
      | CorsOrigins            |
      | GrantTypes             |
      | IdPRestrictions        |
      | PostLogoutRedirectUris |
      | Properties             |
      | RedirectUris           |
      | Scopes                 |
      | Secrets                |
