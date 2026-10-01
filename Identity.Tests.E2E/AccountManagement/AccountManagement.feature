@E2E
Feature: Managing your account
  A signed-in member changes their password or email, sees their session, or deletes their account.

  Scenario: A member changes their password and only the new one works
    Given a signed-in member
    When they change their password
    Then their previous password is refused in a new session
    And they can sign in with their current details in a new session

  Scenario: A member deletes their account and can no longer sign in
    Given a signed-in member
    When they delete their account
    Then signing in to their account in a new session is refused

  Scenario: A member changes their email and signs in with the new address only
    Given a signed-in member
    When they change their email and confirm the new address
    Then they can sign in with their current details in a new session
    And their previous email is refused in a new session

  Scenario: Changing to the email already on the account sends nothing
    Given a signed-in member
    When they change their email to the one already on the account
    Then they are told the email is unchanged
    And no confirmation email is sent

  Scenario: A signed-in member sees the claims their session carries
    Given a signed-in member
    When they open their session diagnostics
    Then they see the claims their session carries
