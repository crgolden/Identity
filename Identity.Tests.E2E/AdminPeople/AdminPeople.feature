@E2E
Feature: Managing users and roles
  An administrator looks after member accounts and the roles that grant them access.

  Scenario: An administrator finds their own account in the user list
    Given a signed-in administrator
    When they open the user list
    Then their own account is listed

  Scenario: An administrator opens a user and can reach every part of the account
    Given a signed-in administrator
    When they open their own account
    Then they can reach the account's claims, roles, logins and passkeys

  Scenario Outline: An administrator opens one part of a user's account
    Given a signed-in administrator
    And they have opened their own account
    When they open the account's <part>
    Then the account's <part> are shown

    Examples:
      | part     |
      | Claims   |
      | Logins   |
      | Passkeys |

  Scenario: A user's roles page shows every role the user holds
    Given a signed-in administrator
    And they have opened their own account
    When they open the roles the account holds
    Then there is one row for each of those roles

  Scenario: An administrator changes a user's phone number and it is kept
    Given a signed-in administrator
    And they have opened their own account
    When they change the account's phone number
    Then the new phone number is kept

  Scenario: An administrator adds a claim to a user
    Given a signed-in administrator
    And they have opened their own account
    When they add a claim to the account
    Then the account holds the new claim

  Scenario: An administrator removes a claim from a user
    Given a signed-in administrator
    And they have opened their own account
    And the account holds a claim
    When they remove that claim from the account
    Then the account no longer holds the claim

  Scenario: An administrator changes a claim on a user
    Given a signed-in administrator
    And they have opened their own account
    And the account holds a claim
    When they change that claim's value on the account
    Then the account's claim has the new value

  Scenario: An administrator gives a user a role
    Given a signed-in administrator
    And a role
    And they have opened their own account
    When they give the account that role
    Then the account holds the role

  Scenario: An administrator takes a role away from a user
    Given a signed-in administrator
    And a role
    And they have opened their own account
    And the account holds that role
    When they take that role away from the account
    Then the account no longer holds the role

  Scenario: An administrator swaps a user's role for another
    Given a signed-in administrator
    And a role
    And another role
    And they have opened their own account
    And the account holds that role
    When they swap that role for the other role
    Then the account holds only the other role

  Scenario: An administrator sees the admin role in the role list
    Given a signed-in administrator
    When they open the role list
    Then the admin role is listed

  Scenario: An administrator creates a role and then deletes it
    Given a signed-in administrator
    When they create a role
    And they delete that role from the role list
    Then the role is no longer listed

  Scenario: An administrator opens a role and can reach its claims and users
    Given a signed-in administrator
    When they open the admin role
    Then they can reach the role's claims and users and can edit or delete it

  Scenario: An administrator opens a role's claims
    Given a signed-in administrator
    And they have opened the admin role
    When they open the role's claims
    Then the role's claims are shown

  Scenario: An administrator sees who holds a role
    Given a signed-in administrator
    And they have opened the admin role
    When they open the role's users
    Then their own account is among the role's users

  Scenario: An administrator renames a role and the new name is kept
    Given a signed-in administrator
    And a role
    When they rename that role
    Then the role keeps its identity under the new name

  Scenario: An administrator adds a claim to a role
    Given a signed-in administrator
    And a role
    When they add a claim to the role
    Then the role holds the new claim

  Scenario: An administrator removes a claim from a role
    Given a signed-in administrator
    And a role
    And the role holds a claim
    When they remove that claim from the role
    Then the role no longer holds the claim

  Scenario: An administrator changes a claim on a role
    Given a signed-in administrator
    And a role
    And the role holds a claim
    When they change that claim's value on the role
    Then the role's claim has the new value
