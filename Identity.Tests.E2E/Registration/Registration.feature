@E2E
Feature: Registration
  A visitor creates an account and confirms their email before they can sign in.

  Scenario: A visitor registers, confirms their email and signs in
    When a visitor registers an account
    And they confirm their email from the message they were sent
    Then they can sign in with their current details in a new session

  Scenario: A newly registered member cannot sign in before confirming their email
    When a visitor registers an account
    Then signing in to their account in a new session is refused

  Scenario: Registering with an email already in use is refused
    Given a visitor has registered an account
    When another visitor registers with the same email
    Then the registration is refused with the reason shown

  Scenario: A new member asks for another confirmation email and uses it to confirm
    Given a visitor has registered an account
    And they have set aside the first confirmation message
    When they ask for another confirmation email
    And they confirm their email from the message they were sent
    Then they can sign in with their current details in a new session
