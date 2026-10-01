@E2E
Feature: Sign in
  A registered member signs in with their email and password, and the account is protected
  against repeated guessing.

  Scenario: A member signs in with their password
    Given a member with a confirmed account
    When they sign in with their password
    Then they are signed in

  Scenario: A wrong password is refused
    Given a member with a confirmed account
    When they sign in with a wrong password
    Then they stay on the sign-in page with the reason shown

  Scenario: An empty form is caught before it is sent
    Given a visitor on the sign-in page
    When they submit the form without filling it in
    Then the email field asks for a value
    And nothing is sent to the server
    And the page raises no script error

  Scenario: Repeated wrong passwords lock the account
    Given a member with a confirmed account
    When they enter a wrong password as many times as the lockout policy allows
    Then they are told the account is locked

  Scenario: A member signs out and must sign in again to manage their account
    Given a signed-in member
    When they sign out
    Then opening their account sends them to sign in

  Scenario Outline: A sign-in link that points at another site keeps the member on this site
    Given a member with a confirmed account
    When they sign in from a link that returns them to <return address>
    Then they are signed in
    And they are still on this site

    Examples:
      | return address         |
      | https%3A%2F%2Fevil.com |
      | %2F%2Fevil.com         |

  Scenario: A member who signs in from a protected page lands back on it
    Given a member with a confirmed account
    When they sign in from a link that returns them to their account page
    Then they land on their account page
