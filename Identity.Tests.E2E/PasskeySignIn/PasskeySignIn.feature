@E2E
Feature: Passkey sign in
  A member adds a passkey to their account and signs in with it instead of a password.

  Scenario: A member adds a passkey and then signs in with it
    Given a member signed in on a browser that can hold a passkey
    When they add a passkey to their account
    And they sign out and sign in again with the passkey
    Then they are signed in
