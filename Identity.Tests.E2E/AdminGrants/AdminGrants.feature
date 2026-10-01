@E2E
Feature: Managing issued grants
  An administrator can inspect the grants members have given applications and revoke them.

  Scenario: An administrator inspects a grant a member gave and deletes it
    Given a member has approved an application
    And a signed-in administrator
    When they open that grant from the grant list
    Then they see the grant's details
    When they delete the grant
    Then the grant is no longer listed
