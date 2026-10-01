@E2E
Feature: Signing in with an authenticator
  A member protects their account with an authenticator app and keeps recovery codes for when they lose it.

  Scenario: A member sets up an authenticator app
    Given a signed-in member
    When they set up an authenticator app
    Then two-factor sign-in is on for their account

  Scenario: A member with two-factor on signs in with a recovery code
    Given a signed-in member
    And they have set up an authenticator app
    And they have generated recovery codes
    When they sign in with a recovery code in a new session
    Then the new session is signed in

  Scenario: A member resets their authenticator and is taken back to set it up
    Given a signed-in member
    And they have set up an authenticator app
    When they reset their authenticator
    Then they are taken back to set up an authenticator

  Scenario: A member turns off two-factor sign-in and is no longer asked for a code
    Given a signed-in member
    And they have set up an authenticator app
    And signing in again in a new session asks for a code
    When they sign in with a code in a new session and turn two-factor sign-in off
    Then signing in again in a new session asks for no code
