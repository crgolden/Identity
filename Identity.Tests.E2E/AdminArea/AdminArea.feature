@E2E
Feature: Admin area
  Administrators reach every configuration section from one landing page; members never see the way in.

  Scenario: An administrator sees the way into the admin area
    Given a signed-in administrator
    When they open the home page
    Then the navigation offers the admin area

  Scenario: A member sees no way into the admin area
    Given a signed-in member
    When they open the home page
    Then the navigation greets the member
    And the navigation offers no admin area

  Scenario: An administrator sees a card for every admin section
    Given a signed-in administrator
    When they open the admin area
    Then there is a card for every admin section

  Scenario Outline: An administrator follows a section card to its list
    Given a signed-in administrator
    When they follow the card for the "<section>" section from the admin area
    Then that section opens its own list

    Examples:
      | section                             |
      | Clients                             |
      | API Resources                       |
      | API Scopes                          |
      | Identity Resources                  |
      | Identity Providers                  |
      | SAML Service Providers              |
      | Persisted Grants                    |
      | Device Flow Codes                   |
      | Server-Side Sessions                |
      | Keys                                |
      | Pushed Authorization Requests       |
      | SAML Sign-In States                 |
      | SAML Logout Sessions                |
      | SAML Logout Session Request Indices |
      | Users                               |
      | Roles                               |
