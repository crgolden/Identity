@E2E
Feature: Home page
  The home page offers a visitor a way in, and a signed-in member a way to their account.

  Scenario: A visitor is invited to register or sign in
    When a visitor opens the home page
    Then they are offered to create an account or sign in
    And they are not offered account links

  Scenario: A signed-in member is offered their account instead
    Given a signed-in member
    When they open the home page
    Then they are offered their account and the applications they allowed
    And they are not offered to create an account or sign in

  Scenario: A signed-in member sees who is signed in
    Given a signed-in member
    When they open the home page
    Then the page names the member who is signed in

  Scenario: An administrator is offered the admin area
    Given a signed-in administrator
    When they open the home page
    Then they are offered the admin area

  Scenario: A member is not offered the admin area
    Given a signed-in member
    When they open the home page
    Then they are offered their account and the applications they allowed
    And they are not offered the admin area
