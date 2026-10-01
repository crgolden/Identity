@E2E
Feature: Google sign in
  A visitor can create an account with Google, and a member can link Google to the account they already have.

  Scenario: A visitor signs up with a verified Google account and is signed in straight away
    Given a Google account with a verified email and a full profile
    When they sign up with that Google account
    Then they are signed in
    And their new account keeps the Google profile

  Scenario: A visitor signs up with an unverified Google account and is asked to confirm their email
    Given a Google account whose email is not verified
    When they sign up with that Google account
    Then they are asked to confirm their email
    And their new account is not yet confirmed

  Scenario: Signing up with Google using an email already registered is refused with advice to link instead
    Given a member with a confirmed account
    And a Google account with the member's email
    When they sign up with that Google account
    Then they are told to link Google to their existing account instead
    And their account has no Google login

  Scenario: A signed-in member links their Google account
    Given a signed-in member
    And the member already has a given name
    And a Google account with the member's email, another given name and a surname
    When they link that Google account from their account
    Then their account gains a Google login and the surname
    And the given name they already had is kept
