@E2E
Feature: Password reset
  A member who forgot their password sets a new one from an emailed link.

  Scenario: A member who forgot their password resets it by email and signs in with the new one
    Given a member with a confirmed account
    When they reset their password through the emailed link
    Then they can sign in with their current details in a new session

  Scenario: After a reset, the old password is refused
    Given a member with a confirmed account
    When they reset their password through the emailed link
    Then their previous password is refused in a new session
