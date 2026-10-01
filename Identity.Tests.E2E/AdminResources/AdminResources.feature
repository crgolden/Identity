@E2E
Feature: Managing resources
  An administrator defines the APIs, scopes and identity data that clients may request, and maintains their lists.

  Scenario Outline: An administrator finds a record in its section's list
    Given a signed-in administrator
    And an existing <kind>
    When they open the <kind> section
    Then the <kind> appears in its section

    Examples:
      | kind             |
      | ApiResource      |
      | ApiScope         |
      | IdentityResource |

  Scenario Outline: An administrator creates a record and lands on its details
    Given a signed-in administrator
    When they create a new <kind>
    Then they see the details of the new <kind>

    Examples:
      | kind             |
      | ApiResource      |
      | ApiScope         |
      | IdentityResource |

  Scenario Outline: An administrator deletes a record and it leaves the list
    Given a signed-in administrator
    And an existing <kind>
    When they delete the <kind> from its section
    Then the <kind> no longer appears in its section

    Examples:
      | kind             |
      | ApiResource      |
      | ApiScope         |
      | IdentityResource |

  Scenario Outline: An administrator adds an entry to a record's list
    Given a signed-in administrator
    And an existing <kind>
    When they add an entry to its <collection>
    Then its <collection> show the new entry

    Examples:
      | kind             | collection |
      | ApiResource      | Scopes     |
      | ApiResource      | Secrets    |
      | ApiResource      | Properties |
      | ApiResource      | ClaimTypes |
      | ApiScope         | ClaimTypes |
      | ApiScope         | Properties |
      | IdentityResource | ClaimTypes |
      | IdentityResource | Properties |

  Scenario Outline: An administrator removes an entry from a record's list
    Given a signed-in administrator
    And an existing <kind>
    And its <collection> hold an entry
    When they remove that entry
    Then its <collection> no longer show the entry

    Examples:
      | kind             | collection |
      | ApiResource      | Scopes     |
      | ApiResource      | Secrets    |
      | ApiResource      | Properties |
      | ApiResource      | ClaimTypes |
      | ApiScope         | ClaimTypes |
      | ApiScope         | Properties |
      | IdentityResource | ClaimTypes |
      | IdentityResource | Properties |

  Scenario Outline: An administrator changes an entry in a record's list
    Given a signed-in administrator
    And an existing <kind>
    And its <collection> hold an entry
    When they change that entry
    Then its <collection> show the changed entry

    Examples:
      | kind             | collection |
      | ApiResource      | Scopes     |
      | ApiResource      | Secrets    |
      | ApiResource      | Properties |
      | ApiResource      | ClaimTypes |
      | ApiScope         | ClaimTypes |
      | ApiScope         | Properties |
      | IdentityResource | ClaimTypes |
      | IdentityResource | Properties |
