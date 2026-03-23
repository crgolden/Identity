# Testing Documentation

Maps every application path to the tests that cover it.

> **Shell note:** test commands that set environment variables inline use bash syntax. On Windows, use Git Bash, WSL, or set the variables separately before running `dotnet test`.

**Test types**
- **Unit** — xUnit page-model / service / API tests (`Category=Unit`) — 447 tests; includes property-based (`PropertyBased/`) and resilience (`Resilience/`) sub-folders
- **E2E** — Playwright browser tests (`Category=E2E`); includes OIDC discovery tests (`Oidc/`) and IdentityServer flow tests (`ConsentTests`, `GrantsTests`, `DiagnosticsTests`, `ServerSideSessionsTests`)
- **Load** — throughput / failure-rate tests using `Parallel.ForEachAsync` + `HttpClient` (`Category=Load`); run separately (requires live server)

**Coverage legend**
| Symbol | Meaning |
|---|---|
| ✅ | Unit **and** E2E coverage |
| 🟡 | Unit tests only |
| 🔵 | Constructor / instantiation test only |
| ❌ | No tests |

---

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [Registration & Email Confirmation](#2-registration--email-confirmation)
3. [Authentication — Login](#3-authentication--login)
4. [Password Recovery](#4-password-recovery)
5. [Two-Factor Authentication](#5-two-factor-authentication)
6. [Passkeys (WebAuthn)](#6-passkeys-webauthn)
7. [External Login — Google OIDC](#7-external-login--google-oidc)
8. [Account Management — Profile & Phone](#8-account-management--profile--phone)
9. [Account Management — Email Change](#9-account-management--email-change)
10. [Account Management — Password](#10-account-management--password)
11. [Account Management — External Logins](#11-account-management--external-logins)
12. [Account Management — Personal Data](#12-account-management--personal-data)
13. [Minimal API — Passkey Endpoints](#13-minimal-api--passkey-endpoints)
14. [Services](#14-services)
15. [Root & Utility Pages](#15-root--utility-pages)
16. [IdentityServer UI Pages](#16-identityserver-ui-pages)
17. [Coverage Summary Matrix](#17-coverage-summary-matrix)
18. [Load, Property-Based & Resilience Tests](#18-load-property-based--resilience-tests)
19. [Mutation Testing (Stryker)](#19-mutation-testing-stryker)

---

## 1. Application Overview

```mermaid
flowchart LR
    classDef auth fill:#dbeafe,stroke:#2563eb
    classDef manage fill:#dcfce7,stroke:#16a34a
    classDef recovery fill:#fef9c3,stroke:#ca8a04
    classDef api fill:#f3e8ff,stroke:#9333ea
    classDef service fill:#fce7f3,stroke:#db2777
    classDef root fill:#f1f5f9,stroke:#64748b

    subgraph AuthFlows["Authentication"]
        Register:::auth
        Login:::auth
        ExternalLogin:::auth
        LoginWith2fa:::auth
        LoginWithRecoveryCode:::auth
        Lockout:::auth
    end

    subgraph RecoveryFlows["Password Recovery"]
        ForgotPassword:::recovery
        ResetPassword:::recovery
    end

    subgraph ManageFlows["Account Management"]
        Profile:::manage
        Email:::manage
        ChangePassword:::manage
        TwoFA["TwoFactorAuthentication"]:::manage
        Passkeys:::manage
        ExtLogins["ExternalLogins"]:::manage
        PersonalData:::manage
    end

    subgraph APIs["Minimal APIs"]
        PasskeyCreationOpts["POST /Account/PasskeyCreationOptions"]:::api
        PasskeyRequestOpts["POST /Account/PasskeyRequestOptions"]:::api
    end

    subgraph Svc["Services"]
        Gravatar:::service
        EmailSvc["EmailSender (Resend)"]:::service
    end

    Register -->|"confirm email"| Login
    Login --> LoginWith2fa
    Login --> LoginWithRecoveryCode
    Login --> Lockout
    Login --> ExternalLogin
    Login -->|"passkey sign-in"| PasskeyRequestOpts
    ForgotPassword --> ResetPassword
    Profile --> Gravatar
    Register --> EmailSvc
    ForgotPassword --> EmailSvc
    Email --> EmailSvc
    Passkeys -->|"WebAuthn creation"| PasskeyCreationOpts
```

---

## 2. Registration & Email Confirmation

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    RegisterGET["GET /Account/Register"]:::unitOnly

    RegisterPOST{"POST /Account/Register"}

    InvalidModel["Return page"]:::unitOnly

    CreateUser{"CreateUser\nsucceeds?"}

    RequireConfirm{"RequireConfirmed\nAccount?"}

    RegConfirm["GET /Account/RegisterConfirmation"]:::covered

    EmailSent["EmailSender sends confirmation link"]:::covered

    SignInDirect["Signed in directly (no confirm required)"]:::unitOnly

    ConfirmEmailGET["GET /Account/ConfirmEmail"]:::covered

    ConfirmSuccess["Email confirmed ➜ /Index"]:::covered

    ConfirmFail["Error message on page"]:::unitOnly

    NullParams["Redirect to /Index"]:::unitOnly

    ResendGET["GET /Account/ResendEmailConfirmation"]:::partial

    ResendPOST["POST /Account/ResendEmailConfirmation"]:::partial

    RegisterGET --> RegisterPOST
    RegisterPOST -->|"invalid"| InvalidModel
    InvalidModel --> RegisterGET
    RegisterPOST --> CreateUser
    CreateUser -->|"yes"| RequireConfirm
    CreateUser -->|"no — errors shown"| RegisterGET
    RequireConfirm -->|"true"| RegConfirm
    RequireConfirm -->|"false"| SignInDirect
    RegConfirm --> EmailSent
    EmailSent --> ConfirmEmailGET
    ConfirmEmailGET -->|"null userId or code"| NullParams
    ConfirmEmailGET -->|"valid token"| ConfirmSuccess
    ConfirmEmailGET -->|"invalid token"| ConfirmFail
    RegConfirm -->|"resend link"| ResendGET
    ResendGET --> ResendPOST
```

### Registration Tests

| Path | File | Test Method |
|---|---|---|
| GET /Account/Register — return URL variants | `Register.cshtmlTests.cs` | `OnGetAsync_VariousReturnUrlValues_AssignsReturnUrlAndDoesNotThrow` |
| GET /Account/Register — external scheme population | `Register.cshtmlTests.cs` | `OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins` |
| POST /Account/Register — invalid model | `Register.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST /Account/Register — create with RequireConfirmed | `Register.cshtmlTests.cs` | `OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount` |
| GET /Account/ConfirmEmail — null params redirect | `ConfirmEmail.cshtmlTests.cs` | `OnGetAsync_NullOrWhitespaceUserIdOrCode_RedirectsToIndex` |
| GET /Account/ConfirmEmail — constructor | `ConfirmEmail.cshtmlTests.cs` | `ConfirmEmailModel_Constructor_UserManagerNull_DoesNotThrowAndStatusMessageIsNull` |
| Full register → confirm → login | `RegistrationTests.cs` (E2E) | `Register_ConfirmEmail_Login_Succeeds` |
| E2E: resend confirmation + confirm with new link | `AccountManagementTests.cs` (E2E) | `ResendEmailConfirmation_NewLink_ConfirmsAccount` |

---

## 3. Authentication — Login

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    LoginGET["GET /Account/Login"]:::unitOnly

    LoginPOST{"POST /Account/Login\nMethod?"}

    PasswordPath{"PasswordSignInAsync\nresult"}

    PasskeyPath["POST (passkey credential JSON)"]:::unitOnly

    ModelInvalid["Return page (no sign-in)"]:::unitOnly

    SignInSuccess["Redirect to returnUrl / /"]:::covered

    Requires2FA["Redirect to /Account/LoginWith2fa"]:::covered

    LockedOut["Redirect to /Account/Lockout"]:::covered

    SignInFailed["Return page with error"]:::covered

    LockoutPage["GET /Account/Lockout"]:::partial

    LoginWith2faPage["GET /Account/LoginWith2fa"]:::unitOnly

    LoginWith2faPOST{"POST /Account/LoginWith2fa\nresult"}

    TwoFASuccess["Redirect authenticated"]:::covered

    TwoFAInvalid["Return page with error"]:::unitOnly

    TwoFANoUser["Throws InvalidOperationException"]:::unitOnly

    RecoveryLink["Link to /Account/LoginWithRecoveryCode"]

    RecoveryGET["GET /Account/LoginWithRecoveryCode"]:::unitOnly

    RecoveryPOST{"POST /Account/LoginWithRecoveryCode\nresult"}

    RecoverySuccess["Redirect authenticated"]:::covered

    RecoveryInvalid["Return page with error"]:::unitOnly

    LogoutGET["GET /Account/Logout"]:::covered

    LogoutPOST["POST /Account/Logout"]:::covered

    SignedOut["Redirect to returnUrl / /"]

    LoginGET --> LoginPOST
    LoginPOST -->|"password"| PasswordPath
    LoginPOST -->|"passkey"| PasskeyPath
    LoginPOST -->|"ModelState invalid"| ModelInvalid
    PasswordPath -->|"Succeeded"| SignInSuccess
    PasswordPath -->|"RequiresTwoFactor"| Requires2FA
    PasswordPath -->|"IsLockedOut"| LockedOut
    PasswordPath -->|"Failed"| SignInFailed
    PasskeyPath --> SignInSuccess
    LockedOut --> LockoutPage
    Requires2FA --> LoginWith2faPage
    LoginWith2faPage --> LoginWith2faPOST
    LoginWith2faPage --> RecoveryLink
    LoginWith2faPOST -->|"Succeeded"| TwoFASuccess
    LoginWith2faPOST -->|"Invalid code"| TwoFAInvalid
    LoginWith2faPOST -->|"No 2FA user"| TwoFANoUser
    RecoveryLink --> RecoveryGET
    RecoveryGET --> RecoveryPOST
    RecoveryPOST -->|"Succeeded"| RecoverySuccess
    RecoveryPOST -->|"Invalid"| RecoveryInvalid
    SignInSuccess --> LogoutGET
    SignInSuccess --> LogoutPOST
    LogoutGET --> SignedOut
    LogoutPOST --> SignedOut
```

### Login Tests

| Path | File | Test Method |
|---|---|---|
| GET /Account/Login — error message | `Login.cshtmlTests.cs` | `OnGetAsync_WithErrorMessage_AddsModelError` |
| GET /Account/Login — no error | `Login.cshtmlTests.cs` | `OnGetAsync_WithoutErrorMessage_DoesNotAddModelError` |
| GET /Account/Login — return URL | `Login.cshtmlTests.cs` | `OnGetAsync_WithReturnUrl_SetsReturnUrl` |
| GET /Account/Login — default return URL | `Login.cshtmlTests.cs` | `OnGetAsync_WithoutReturnUrl_DefaultsToRoot` |
| GET /Account/Login — external schemes | `Login.cshtmlTests.cs` | `OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins` |
| POST — password success | `Login.cshtmlTests.cs` | `OnPostAsync_PasswordSignIn_Succeeded_ReturnsLocalRedirect` |
| POST — requires 2FA | `Login.cshtmlTests.cs` | `OnPostAsync_PasswordSignIn_RequiresTwoFactor_RedirectsToLoginWith2fa` |
| POST — locked out | `Login.cshtmlTests.cs` | `OnPostAsync_PasswordSignIn_IsLockedOut_RedirectsToLockout` |
| POST — password failed | `Login.cshtmlTests.cs` | `OnPostAsync_PasswordSignIn_Failed_ReturnsPageWithModelError` |
| POST — passkey success | `Login.cshtmlTests.cs` | `OnPostAsync_PasskeySignIn_Succeeded_ReturnsLocalRedirect` |
| POST — invalid model | `Login.cshtmlTests.cs` | `OnPostAsync_InvalidModelState_ReturnsPageWithoutSignIn` |
| GET /Account/LoginWith2fa — valid state | `LoginWith2fa.cshtmlTests.cs` | `OnGetAsync_TwoFactorUserExists_SetsReturnUrlAndReturnsPage` |
| GET /Account/LoginWith2fa — null user | `LoginWith2fa.cshtmlTests.cs` | `OnGetAsync_UserIsNull_ThrowsInvalidOperationException` |
| POST /Account/LoginWith2fa — success | `LoginWith2fa.cshtmlTests.cs` | `OnPostAsync_Succeeds_RedirectsAndSetsStatusMessageAndLogs` |
| POST /Account/LoginWith2fa — invalid code | `LoginWith2fa.cshtmlTests.cs` | `OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage` |
| POST /Account/LoginWith2fa — no 2FA user | `LoginWith2fa.cshtmlTests.cs` | `OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException` |
| GET /Account/LoginWithRecoveryCode | `LoginWithRecoveryCode.cshtmlTests.cs` | `OnGetAsync_ValidUser_SetsPropertiesAndReturnsPageResult` |
| POST /Account/LoginWithRecoveryCode — invalid | `LoginWithRecoveryCode.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPageResult` |
| GET /Account/Logout — redirect to returnUrl | `Logout.cshtmlTests.cs` | `OnGetAsync_VariousReturnUrls_RedirectsCorrectly` |
| POST /Account/Logout — redirect to returnUrl | `Logout.cshtmlTests.cs` | `OnPost_VariousReturnUrls_RedirectsCorrectly` |
| E2E: logout clears session | `AccountManagementTests.cs` (E2E) | `Logout_Succeeds_ProtectedPageRedirectsToLogin` |
| E2E: valid credentials | `LoginTests.cs` (E2E) | `Login_ValidCredentials_Succeeds` |
| E2E: wrong password | `LoginTests.cs` (E2E) | `Login_WrongPassword_ShowsError` |
| E2E: lockout after 5 failures | `LoginTests.cs` (E2E) | `Login_FiveFailedAttempts_LocksAccount` |
| E2E: TOTP 2FA login | `TwoFactorTests.cs` (E2E) | `TwoFactor_Setup_Login_WithTotpCode_Succeeds` |
| E2E: recovery code login | `TwoFactorTests.cs` (E2E) | `TwoFactor_Login_WithRecoveryCode_Succeeds` |

---

## 4. Password Recovery

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    ForgotGET["GET /Account/ForgotPassword"]:::unitOnly

    ForgotPOST{"POST /Account/ForgotPassword"}

    ForgotInvalid["Return page"]:::unitOnly

    ForgotNoUser["Redirect to Confirmation (no email sent — security)"]:::unitOnly

    ForgotSendEmail["Generate token, send reset link"]:::covered

    ForgotConfirmPage["GET /Account/ForgotPasswordConfirmation"]:::partial

    ResetGET{"GET /Account/ResetPassword\ncode present?"}

    ResetNullCode["Returns BadRequest"]:::unitOnly

    ResetMalformed["Throws FormatException"]:::unitOnly

    ResetForm["Show reset form"]:::unitOnly

    ResetPOST{"POST /Account/ResetPassword"}

    ResetInvalid["Return page"]:::unitOnly

    ResetNoUser["Redirect to Confirmation (no error revealed)"]:::unitOnly

    ResetFails["Return page with errors"]:::unitOnly

    ResetSuccess["Redirect to /Account/ResetPasswordConfirmation"]:::covered

    ResetConfirmPage["GET /Account/ResetPasswordConfirmation"]:::partial

    ForgotGET --> ForgotPOST
    ForgotPOST -->|"invalid"| ForgotInvalid
    ForgotInvalid --> ForgotGET
    ForgotPOST -->|"user not found\nor unconfirmed"| ForgotNoUser
    ForgotPOST -->|"user found\nand confirmed"| ForgotSendEmail
    ForgotNoUser --> ForgotConfirmPage
    ForgotSendEmail --> ForgotConfirmPage
    ForgotConfirmPage --> ResetGET
    ResetGET -->|"no code"| ResetNullCode
    ResetGET -->|"malformed code"| ResetMalformed
    ResetGET -->|"valid code"| ResetForm
    ResetForm --> ResetPOST
    ResetPOST -->|"invalid model"| ResetInvalid
    ResetInvalid --> ResetForm
    ResetPOST -->|"user not found"| ResetNoUser
    ResetPOST -->|"reset fails"| ResetFails
    ResetFails --> ResetForm
    ResetPOST -->|"success"| ResetSuccess
    ResetNoUser --> ResetConfirmPage
    ResetSuccess --> ResetConfirmPage
```

### Password Recovery Tests

| Path | File | Test Method |
|---|---|---|
| POST — invalid model | `ForgotPassword.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST — user null or unconfirmed | `ForgotPassword.cshtmlTests.cs` | `OnPostAsync_UserNullOrUnconfirmed_RedirectsToConfirmation_DoesNotSendEmail` |
| GET /ResetPassword — no code | `ResetPassword.cshtmlTests.cs` | `OnGet_CodeIsNull_ReturnsBadRequestWithMessage` |
| GET /ResetPassword — malformed code | `ResetPassword.cshtmlTests.cs` | `OnGet_MalformedCode_ThrowsFormatException` |
| GET /ResetPassword — valid code | `ResetPassword.cshtmlTests.cs` | `OnGet_ValidBase64UrlEncodedCode_SetsInputCodeAndReturnsPage` |
| POST /ResetPassword — invalid model | `ResetPassword.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST /ResetPassword — reset fails | `ResetPassword.cshtmlTests.cs` | `OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage` |
| POST /ResetPassword — user missing or success | `ResetPassword.cshtmlTests.cs` | `OnPostAsync_UserMissingOrResetSucceeds_RedirectsToConfirmation` |
| E2E: full forgot/reset flow | `PasswordResetTests.cs` (E2E) | `ForgotPassword_Reset_LoginWithNewPassword_Succeeds` |

---

## 5. Two-Factor Authentication

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    TwoFAPage["GET /Account/Manage/TwoFactorAuthentication"]:::unitOnly

    EnableAuthGET{"GET /Account/Manage/EnableAuthenticator\nuser found?"}

    EnableNotFound["NotFoundObjectResult"]:::unitOnly

    EnableForm["Show QR code + shared key"]:::covered

    EnablePOST{"POST /Account/Manage/EnableAuthenticator"}

    EnableUserNotFound["NotFoundObjectResult"]:::unitOnly

    EnableInvalid["Return page with error"]:::unitOnly

    EnableSuccess["Redirect to ShowRecoveryCodes"]:::covered

    ShowCodes{"GET /Account/Manage/ShowRecoveryCodes\ncodes present?"}

    ShowCodesEmpty["Redirect to /Account/Manage/TwoFactorAuthentication"]:::unitOnly

    ShowCodesPage["Display recovery codes"]:::covered

    GenCodesGET{"GET /Account/Manage/GenerateRecoveryCodes\nstate check"}

    GenCodesNotFound["NotFoundObjectResult"]:::unitOnly

    Gen2faDisabled["Throws InvalidOperationException"]:::unitOnly

    GenCodesPOST["POST — generate new codes"]:::covered

    Disable2faGET{"GET /Account/Manage/Disable2fa\nstate check"}

    DisableNotFound["NotFoundObjectResult"]:::unitOnly

    Disable2faNotEnabled["Throws InvalidOperationException"]:::unitOnly

    Disable2faForm["Show confirmation form"]:::unitOnly

    Disable2faPOST{"POST /Account/Manage/Disable2fa"}

    DisableFails["Throws InvalidOperationException"]:::unitOnly

    DisableSuccess["Redirect to /Account/Manage/TwoFactorAuthentication"]:::unitOnly

    ResetAuthGET["GET /Account/Manage/ResetAuthenticator"]:::unitOnly

    ResetAuthPOST["POST — reset authenticator"]:::unitOnly

    TwoFAPage -->|"setup TOTP"| EnableAuthGET
    TwoFAPage -->|"generate new codes"| GenCodesGET
    TwoFAPage -->|"disable 2FA"| Disable2faGET
    TwoFAPage -->|"reset authenticator"| ResetAuthGET
    EnableAuthGET -->|"not found"| EnableNotFound
    EnableAuthGET -->|"found"| EnableForm
    EnableForm --> EnablePOST
    EnablePOST -->|"user not found"| EnableUserNotFound
    EnablePOST -->|"invalid code"| EnableInvalid
    EnableInvalid --> EnableForm
    EnablePOST -->|"valid code"| EnableSuccess
    EnableSuccess --> ShowCodes
    ShowCodes -->|"empty"| ShowCodesEmpty
    ShowCodesEmpty --> TwoFAPage
    ShowCodes -->|"has codes"| ShowCodesPage
    GenCodesGET -->|"not found"| GenCodesNotFound
    GenCodesGET -->|"2FA disabled"| Gen2faDisabled
    GenCodesGET -->|"2FA enabled"| GenCodesPOST
    GenCodesPOST --> ShowCodes
    Disable2faGET -->|"user not found"| DisableNotFound
    Disable2faGET -->|"2FA not enabled"| Disable2faNotEnabled
    Disable2faGET -->|"2FA enabled"| Disable2faForm
    Disable2faForm --> Disable2faPOST
    Disable2faPOST -->|"fails"| DisableFails
    Disable2faPOST -->|"success"| DisableSuccess
    DisableSuccess --> TwoFAPage
    ResetAuthGET --> ResetAuthPOST
    ResetAuthPOST --> TwoFAPage
```

### 2FA Tests

| Path | File | Test Method |
|---|---|---|
| GET /TwoFactorAuthentication — user found | `TwoFactorAuthentication.cshtmlTests.cs` | `OnGetAsync_UserFound_SetsPropertiesAndReturnsPageResult` |
| GET /TwoFactorAuthentication — user not found | `TwoFactorAuthentication.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| GET /EnableAuthenticator — user not found | `EnableAuthenticator.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage` |
| POST /EnableAuthenticator — user not found | `EnableAuthenticator.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| POST /EnableAuthenticator — invalid code | `EnableAuthenticator.cshtmlTests.cs` | `OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage` |
| POST /EnableAuthenticator — valid, redirect | `EnableAuthenticator.cshtmlTests.cs` | `OnPostAsync_ValidToken_RedirectsBasedOnRecoveryCodesCount` |
| GET /ShowRecoveryCodes — empty | `ShowRecoveryCodes.cshtmlTests.cs` | `OnGet_RecoveryCodesNullOrEmpty_RedirectsToTwoFactorAuthentication` |
| GET /ShowRecoveryCodes — has codes | `ShowRecoveryCodes.cshtmlTests.cs` | `OnGet_RecoveryCodesHasItems_ReturnsPageResult` |
| GET /GenerateRecoveryCodes — user not found | `GenerateRecoveryCodes.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage` |
| POST /GenerateRecoveryCodes — 2FA disabled | `GenerateRecoveryCodes.cshtmlTests.cs` | `OnPostAsync_TwoFactorDisabled_ThrowsInvalidOperationException` |
| POST /GenerateRecoveryCodes — generate | `GenerateRecoveryCodes.cshtmlTests.cs` | `OnPostAsync_TwoFactorEnabled_GeneratesCodesAndRedirects` |
| GET /Disable2fa — state check | `Disable2fa.cshtmlTests.cs` | `OnGet_TwoFactorState_BehavesAsExpected` |
| GET /Disable2fa — user null | `Disable2fa.cshtmlTests.cs` | `OnGet_UserIsNull_ReturnsNotFoundWithUserIdInMessage` |
| POST /Disable2fa — fails | `Disable2fa.cshtmlTests.cs` | `OnPostAsync_DisableFails_ThrowsInvalidOperationException` |
| POST /Disable2fa — success | `Disable2fa.cshtmlTests.cs` | `OnPostAsync_Succeeds_RedirectsAndSetsStatusMessageAndLogs` |
| GET /ResetAuthenticator — user existence | `ResetAuthenticator.cshtmlTests.cs` | `OnGet_UserExistence_ReturnsExpectedResult` |
| POST /ResetAuthenticator | `ResetAuthenticator.cshtmlTests.cs` | `OnPostAsync_UserExists_ResetsAndRedirectsRegardlessOfIdentityResult` |
| POST /ResetAuthenticator — user not found | `ResetAuthenticator.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage` |
| E2E: TOTP setup + login | `TwoFactorTests.cs` (E2E) | `TwoFactor_Setup_Login_WithTotpCode_Succeeds` |
| E2E: recovery code login | `TwoFactorTests.cs` (E2E) | `TwoFactor_Login_WithRecoveryCode_Succeeds` |
| E2E: disable 2FA, subsequent login skips challenge | `Disable2faTests.cs` (E2E) | `Disable2fa_AfterSetup_SubsequentLogin_DoesNotRequire2fa` |

---

## 6. Passkeys (WebAuthn)

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    PasskeysGET{"GET /Account/Manage/Passkeys\nuser found?"}

    PasskeysNotFound["NotFoundObjectResult"]:::unitOnly

    PasskeysList["Show passkey list"]:::unitOnly

    CreationOptsEndpoint{"POST /Account/PasskeyCreationOptions\nuser found?"}

    CreationOpts404["404 Not Found"]:::unitOnly

    CreationOpts200["200 JSON creation options"]:::unitOnly

    WebAuthnCeremony["Browser performs WebAuthn creation ceremony"]:::noTest

    AddPasskeyPOST{"POST /Account/Manage/Passkeys\nAddPasskey handler — user found?"}

    AddNotFound["NotFoundObjectResult"]:::unitOnly

    AttestFails["Redirect with failure message (skipped)"]:::partial

    AddOrUpdateFails["Redirect with failure message (skipped)"]:::partial

    AddSuccess["Redirect to /Account/Manage/RenamePasskey (skipped)"]:::partial

    RenameGET{"GET /Account/Manage/RenamePasskey?id\nstate check"}

    RenameInvalidB64["Redirect to Passkeys + status"]:::unitOnly

    RenameNotFound["NotFoundObjectResult"]:::unitOnly

    RenameForm["Show rename form"]:::unitOnly

    RenamePOST["POST — update passkey name"]:::unitOnly

    UpdatePasskeyPOST["POST /Account/Manage/Passkeys\nUpdatePasskey handler"]:::unitOnly

    RequestOptsEndpoint{"POST /Account/PasskeyRequestOptions\nusername provided?"}

    RequestOptsNull["Options with null user"]:::unitOnly

    RequestOptsUser["Find user, options with user entity"]:::unitOnly

    PasskeySignIn["POST /Account/Login (passkey credential JSON)"]:::unitOnly

    PasskeysGET -->|"not found"| PasskeysNotFound
    PasskeysGET -->|"found"| PasskeysList
    PasskeysList -->|"add passkey"| CreationOptsEndpoint
    CreationOptsEndpoint -->|"not found"| CreationOpts404
    CreationOptsEndpoint -->|"found"| CreationOpts200
    CreationOpts200 --> WebAuthnCeremony
    WebAuthnCeremony --> AddPasskeyPOST
    AddPasskeyPOST -->|"not found"| AddNotFound
    AddPasskeyPOST -->|"attestation fails"| AttestFails
    AddPasskeyPOST -->|"add/update fails"| AddOrUpdateFails
    AddPasskeyPOST -->|"success"| AddSuccess
    AddSuccess --> RenameGET
    RenameGET -->|"invalid base64 id"| RenameInvalidB64
    RenameInvalidB64 --> PasskeysList
    RenameGET -->|"passkey not found"| RenameNotFound
    RenameGET -->|"found"| RenameForm
    RenameForm --> RenamePOST
    RenamePOST --> PasskeysList
    PasskeysList -->|"update passkey"| UpdatePasskeyPOST
    RequestOptsEndpoint -->|"null or whitespace"| RequestOptsNull
    RequestOptsEndpoint -->|"username provided"| RequestOptsUser
    RequestOptsNull --> PasskeySignIn
    RequestOptsUser --> PasskeySignIn
```

### Passkey Tests

| Path | File | Test Method |
|---|---|---|
| `MapAdditionalIdentityEndpoints` null guard | `EndpointRouteBuilderExtensionsTests.cs` | `MapAdditionalIdentityEndpoints_NullEndpoints_ThrowsArgumentNullException` |
| POST /PasskeyCreationOptions — user not found | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserNotFound_Returns404` |
| POST /PasskeyCreationOptions — 200 + JSON | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserFound_ReturnsOkWithJson` |
| POST /PasskeyCreationOptions — user entity | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager` |
| POST /PasskeyRequestOptions — null username | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_NullUsername_MakesRequestOptionsWithNullUser` |
| POST /PasskeyRequestOptions — whitespace | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser` |
| POST /PasskeyRequestOptions — with username | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions` |
| POST /PasskeyRequestOptions — 200 | `EndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_ReturnsOkWithJson` |
| GET /Manage/Passkeys — not found | `Passkeys.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| GET /Manage/Passkeys — constructor | `Passkeys.cshtmlTests.cs` | `PasskeysModel_Ctor_ValidManagers_PropertiesInitializedToNull` |
| AddPasskey — user not found | `Passkeys.cshtmlTests.cs` | `OnPostAddPasskeyAsync_UserNotFound_ReturnsNotFound` |
| AddPasskey — attestation fails (skipped) | `Passkeys.cshtmlTests.cs` | `OnPostAddPasskeyAsync_AttestationFails_RedirectsWithFailureMessage_Partial` |
| AddPasskey — add/update fails (skipped) | `Passkeys.cshtmlTests.cs` | `OnPostAddPasskeyAsync_AddOrUpdateFails_RedirectsWithFailureMessage_Partial` |
| AddPasskey — success (skipped) | `Passkeys.cshtmlTests.cs` | `OnPostAddPasskeyAsync_Success_RedirectsToRenamePasskey_Partial` |
| UpdatePasskey — user not found | `Passkeys.cshtmlTests.cs` | `OnPostUpdatePasskeyAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| GET /RenamePasskey — invalid base64 | `RenamePasskey.cshtmlTests.cs` | `OnGetAsync_InvalidBase64Id_RedirectsToPasskeysAndSetsStatusMessage` |
| GET /RenamePasskey — not found | `RenamePasskey.cshtmlTests.cs` | `OnGetAsync_PasskeyNotFound_ReturnsNotFoundWithMessage` |
| POST /RenamePasskey — user not found | `RenamePasskey.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage` |

> **Note:** The WebAuthn browser ceremony (navigator.credentials.create/get) has no automated test coverage; the three skip-marked tests represent intended future coverage.

---

## 7. External Login — Google OIDC

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    LoginPage["GET /Account/Login (Google button visible)"]:::unitOnly

    GoogleChallenge["Challenge Google OIDC provider (browser redirect to Google)"]:::noTest

    GoogleCallback{"GET /Account/ExternalLogin\n?callback — remote error?"}

    RemoteError["Redirect to /Account/Login with error message"]:::unitOnly

    InfoNull["Redirect to /Account/Login with error message"]:::unitOnly

    ExistingUserSignIn["Existing user found — sign in directly"]:::noTest

    ConfirmationForm{"POST /Account/ExternalLogin/Confirmation\nstate check"}

    ConfirmModelInvalid["Return page"]:::unitOnly

    ConfirmInfoNull["Redirect to Login with error"]:::unitOnly

    CreateAndAddLogin{"Create user\n+ AddLogin\nsucceeds?"}

    ConfirmSuccess["Redirect (based on RequireConfirmedAccount)"]:::unitOnly

    ConfirmFail["Return page with errors"]:::noTest

    LoginPage -->|"click Sign in with Google"| GoogleChallenge
    GoogleChallenge --> GoogleCallback
    GoogleCallback -->|"remote error"| RemoteError
    GoogleCallback -->|"info null"| InfoNull
    GoogleCallback -->|"existing user"| ExistingUserSignIn
    GoogleCallback -->|"new user"| ConfirmationForm
    ConfirmationForm -->|"invalid model"| ConfirmModelInvalid
    ConfirmationForm -->|"info null"| ConfirmInfoNull
    ConfirmationForm --> CreateAndAddLogin
    CreateAndAddLogin -->|"success"| ConfirmSuccess
    CreateAndAddLogin -->|"failure"| ConfirmFail
    RemoteError --> LoginPage
    InfoNull --> LoginPage
```

### External Login Tests

| Path | File | Test Method |
|---|---|---|
| GET /Login — external schemes | `Login.cshtmlTests.cs` | `OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins` |
| Callback — remote error | `ExternalLogin.cshtmlTests.cs` | `OnGetCallbackAsync_RemoteErrorProvided_SetsErrorMessageAndRedirectsToLogin` |
| Callback — info null | `ExternalLogin.cshtmlTests.cs` | `OnGetCallbackAsync_InfoIsNull_SetsErrorMessageAndRedirectsToLogin` |
| Confirmation — model invalid | `ExternalLogin.cshtmlTests.cs` | `OnPostConfirmationAsync_ModelStateInvalid_ReturnsPageAndSetsProviderDisplayNameAndReturnUrl` |
| Confirmation — info null | `ExternalLogin.cshtmlTests.cs` | `OnPostConfirmationAsync_InfoNull_ReturnsRedirectToLoginAndSetsErrorMessage` |
| Confirmation — create + add login | `ExternalLogin.cshtmlTests.cs` | `OnPostConfirmationAsync_CreateAndAddLogin_Succeeds_ConditionalRedirect` |

> **Note:** The Google OAuth redirect and the existing-user sign-in path have no automated test coverage (requires live Google OIDC).

---

## 8. Account Management — Profile & Phone

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    ProfileGET{"GET /Account/Manage/Index\nuser found?"}

    ProfileNotFound["NotFoundObjectResult"]:::unitOnly

    ProfilePage["Show profile form (username, phone, Gravatar avatar)"]:::unitOnly

    ProfilePOST{"POST /Account/Manage/Index\nstate check"}

    ProfileUserNotFound["NotFoundObjectResult"]:::unitOnly

    ProfileModelInvalid["Return page"]:::unitOnly

    ProfileSaved["Update phone, RefreshSignIn"]:::covered

    GravatarService["GravatarService.GetAvatarUrlAsync()"]:::unitOnly

    ProfileGET -->|"not found"| ProfileNotFound
    ProfileGET -->|"found"| ProfilePage
    ProfilePage --> GravatarService
    ProfilePage --> ProfilePOST
    ProfilePOST -->|"not found"| ProfileUserNotFound
    ProfilePOST -->|"invalid model"| ProfileModelInvalid
    ProfileModelInvalid --> ProfilePage
    ProfilePOST -->|"success"| ProfileSaved
```

### Profile Tests

| Path | File | Test Method |
|---|---|---|
| GET — user not found | `Manage/Index.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| GET — user found | `Manage/Index.cshtmlTests.cs` | `OnGetAsync_UserExists_LoadsUsernameAndPhoneAndReturnsPage` |
| POST — user not found | `Manage/Index.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage` |
| POST — invalid model | `Manage/Index.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPageAndDoesNotChangePhoneOrSignIn` |
| Gravatar — profile found | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileFound_ReturnsAvatarUrl` |
| Gravatar — not found | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileNotFound_ReturnsNull` |
| Gravatar — null avatar URL | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileReturnsNullAvatarUrl_ReturnsNull` |
| Gravatar — non-404 exception | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_NonNotFoundApiException_PropagatesException` |
| Gravatar — SHA-256 hash casing | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_AlwaysHashesEmailToSha256Lowercase` |
| Gravatar — cancellation | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_PassesCancellationToken` |

---

## 9. Account Management — Email Change

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    EmailGET{"GET /Account/Manage/Email\nuser found?"}

    EmailNotFound["NotFoundObjectResult"]:::unitOnly

    EmailPage["Show current email + change form"]:::unitOnly

    SendVerifPOST{"POST (SendVerificationEmail handler)\nstate check"}

    SendVerifInvalid["Return page"]:::unitOnly

    SendVerifNotFound["NotFoundObjectResult"]:::unitOnly

    SendVerifSuccess["Send email, redirect"]:::unitOnly

    ChangeEmailPOST["POST (ChangeEmail handler)"]:::unitOnly

    ConfirmEmailChangeGET{"GET /Account/ConfirmEmailChange\nparams valid?"}

    ConfirmNull["Redirect to /Account/Manage/Index"]:::unitOnly

    ConfirmEmptyEmail["Redirect to /Account/Manage/Index"]:::unitOnly

    ConfirmChangeFails["Return page with error"]:::unitOnly

    ConfirmSetUserNameFails["Return page with error"]:::unitOnly

    ConfirmChangeSuccess["Change email, RefreshSignIn"]:::unitOnly

    EmailGET -->|"not found"| EmailNotFound
    EmailGET -->|"found"| EmailPage
    EmailPage --> SendVerifPOST
    EmailPage --> ChangeEmailPOST
    SendVerifPOST -->|"invalid model"| SendVerifInvalid
    SendVerifInvalid --> EmailPage
    SendVerifPOST -->|"not found"| SendVerifNotFound
    SendVerifPOST -->|"valid"| SendVerifSuccess
    SendVerifSuccess -->|"email link"| ConfirmEmailChangeGET
    ChangeEmailPOST -->|"email link"| ConfirmEmailChangeGET
    ConfirmEmailChangeGET -->|"null params"| ConfirmNull
    ConfirmNull -->|"redirect"| EmailPage
    ConfirmEmailChangeGET -->|"empty or whitespace email"| ConfirmEmptyEmail
    ConfirmEmptyEmail -->|"redirect"| EmailPage
    ConfirmEmailChangeGET -->|"change email fails"| ConfirmChangeFails
    ConfirmEmailChangeGET -->|"set username fails"| ConfirmSetUserNameFails
    ConfirmEmailChangeGET -->|"success"| ConfirmChangeSuccess
```

### Email Change Tests

| Path | File | Test Method |
|---|---|---|
| GET — constructor defaults | `Manage/Email.cshtmlTests.cs` | `Constructor_ValidDependencies_InitializesDefaults` |
| POST SendVerification — invalid model | `Manage/Email.cshtmlTests.cs` | `OnPostSendVerificationEmailAsync_InvalidModelState_ReturnsPage` |
| POST SendVerification — user not found | `Manage/Email.cshtmlTests.cs` | `OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId` |
| POST SendVerification — success | `Manage/Email.cshtmlTests.cs` | `OnPostSendVerificationEmailAsync_ValidUser_SendsEmailAndRedirects` |
| POST ChangeEmail — user not found | `Manage/Email.cshtmlTests.cs` | `OnPostChangeEmailAsync_UserNotFound_ReturnsNotFound` |
| GET /ConfirmEmailChange — null params | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_NullParameters_RedirectsToIndex` |
| GET /ConfirmEmailChange — special char email | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_SpecialCharacterEmail_ProceedsAndReturnSuccess` |
| GET /ConfirmEmailChange — empty email | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_EmptyOrWhitespaceEmail_RedirectsToIndex` |
| GET /ConfirmEmailChange — change fails | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_ChangeEmailFails_ReturnsPageAndSetsStatusMessage` |
| GET /ConfirmEmailChange — set username fails | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_SetUserNameFails_ReturnsPageAndSetsStatusMessage` |
| GET /ConfirmEmailChange — success | `ConfirmEmailChange.cshtmlTests.cs` | `OnGetAsync_AllOperationsSucceed_RefreshesSignInAndSetsSuccessMessage` |
| E2E: change email, confirm, new email works | `EmailChangeTests.cs` (E2E) | `ChangeEmail_Success_NewEmailConfirmed_OldEmailNoLongerValid` |
| E2E: change email, confirm via link in new browser context | `EmailChangeTests.cs` (E2E) | `ChangeEmail_SameEmail_DoesNotSendConfirmation` |
| E2E: change email end-to-end | `AccountManagementTests.cs` (E2E) | `ChangeEmail_Succeeds_NewEmailWorks` |

---

## 10. Account Management — Password

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    ChangePwdGET{"GET /Account/Manage/ChangePassword\nuser found?"}

    ChangePwdNotFound["NotFoundObjectResult"]:::unitOnly

    ChangePwdForm["Show change password form"]:::unitOnly

    ChangePwdPOST{"POST /Account/Manage/ChangePassword\nstate check"}

    ChangePwdInvalid["Return page"]:::unitOnly

    ChangePwdUserNotFound["NotFoundObjectResult"]:::unitOnly

    ChangePwdSuccess["RefreshSignIn, redirect"]:::covered

    SetPwdGET{"GET /Account/Manage/SetPassword\nstate check"}

    SetPwdNotFound["NotFoundObjectResult"]:::unitOnly

    SetPwdHasPwd["Redirect to ChangePassword"]:::unitOnly

    SetPwdForm["Show set password form"]:::unitOnly

    SetPwdPOST{"POST /Account/Manage/SetPassword\nstate check"}

    SetPwdInvalid["Return page"]:::unitOnly

    SetPwdNotFoundPost["NotFoundObjectResult"]:::unitOnly

    SetPwdSuccess["Set password, RefreshSignIn"]:::unitOnly

    ChangePwdGET -->|"not found"| ChangePwdNotFound
    ChangePwdGET -->|"found"| ChangePwdForm
    ChangePwdForm --> ChangePwdPOST
    ChangePwdPOST -->|"invalid model"| ChangePwdInvalid
    ChangePwdInvalid --> ChangePwdForm
    ChangePwdPOST -->|"user not found"| ChangePwdUserNotFound
    ChangePwdPOST -->|"success"| ChangePwdSuccess
    SetPwdGET -->|"not found"| SetPwdNotFound
    SetPwdGET -->|"has password"| SetPwdHasPwd
    SetPwdHasPwd -->|"redirect"| ChangePwdGET
    SetPwdGET -->|"no password"| SetPwdForm
    SetPwdForm --> SetPwdPOST
    SetPwdPOST -->|"invalid model"| SetPwdInvalid
    SetPwdInvalid --> SetPwdForm
    SetPwdPOST -->|"user not found"| SetPwdNotFoundPost
    SetPwdPOST -->|"success"| SetPwdSuccess
```

### Password Management Tests

| Path | File | Test Method |
|---|---|---|
| GET /ChangePassword — user not found | `Manage/ChangePassword.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| POST /ChangePassword — invalid model | `Manage/ChangePassword.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST /ChangePassword — user not found | `Manage/ChangePassword.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundWithUserId` |
| GET /SetPassword — user not found | `Manage/SetPassword.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdInMessage` |
| GET /SetPassword — hasPassword check | `Manage/SetPassword.cshtmlTests.cs` | `OnGetAsync_ExistingUser_BehavesBasedOnHasPassword` |
| POST /SetPassword — invalid model | `Manage/SetPassword.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST /SetPassword — user not found | `Manage/SetPassword.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage` |
| E2E: change password, old no longer works | `AccountManagementTests.cs` (E2E) | `ChangePassword_Success_OldPasswordNoLongerWorks` |

---

## 11. Account Management — External Logins

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    ExtLoginsGET{"GET /Account/Manage/ExternalLogins\nuser found?"}

    ExtLoginsNotFound["NotFoundObjectResult"]:::unitOnly

    ExtLoginsPage["Show linked / available providers"]:::unitOnly

    LinkLoginPOST["POST LinkLogin handler — challenge provider"]:::unitOnly

    GoogleRedirect["Browser redirects to Google"]:::noTest

    LinkCallbackGET{"GET LinkLoginCallback\nstate check"}

    CallbackNoInfo["Throws InvalidOperationException"]:::unitOnly

    CallbackNotFound["NotFoundObjectResult"]:::unitOnly

    CallbackAddResult["Update status message, redirect"]:::unitOnly

    RemoveLoginPOST{"POST RemoveLogin handler\nuser found?"}

    RemoveNotFound["NotFoundObjectResult"]:::unitOnly

    RemoveFails["Set failure message, redirect"]:::unitOnly

    RemoveSuccess["RefreshSignIn, set success message"]:::unitOnly

    ExtLoginsGET -->|"not found"| ExtLoginsNotFound
    ExtLoginsGET -->|"found"| ExtLoginsPage
    ExtLoginsPage -->|"link provider"| LinkLoginPOST
    LinkLoginPOST --> GoogleRedirect
    GoogleRedirect --> LinkCallbackGET
    LinkCallbackGET -->|"no info"| CallbackNoInfo
    LinkCallbackGET -->|"user not found"| CallbackNotFound
    LinkCallbackGET -->|"add result"| CallbackAddResult
    CallbackAddResult --> ExtLoginsPage
    ExtLoginsPage -->|"remove login"| RemoveLoginPOST
    RemoveLoginPOST -->|"not found"| RemoveNotFound
    RemoveLoginPOST -->|"remove fails"| RemoveFails
    RemoveLoginPOST -->|"success"| RemoveSuccess
    RemoveFails --> ExtLoginsPage
    RemoveSuccess --> ExtLoginsPage
```

### External Logins Management Tests

| Path | File | Test Method |
|---|---|---|
| GET — user not found | `Manage/ExternalLogins.cshtmlTests.cs` | `OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage` |
| POST LinkLogin — challenge | `Manage/ExternalLogins.cshtmlTests.cs` | `OnPostLinkLoginAsync_Provider_ReturnsChallengeAndSignsOut` |
| GET Callback — no info | `Manage/ExternalLogins.cshtmlTests.cs` | `OnGetLinkLoginCallbackAsync_NoExternalLoginInfo_ThrowsInvalidOperationException` |
| GET Callback — user not found | `Manage/ExternalLogins.cshtmlTests.cs` | `OnGetLinkLoginCallbackAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| GET Callback — add login result | `Manage/ExternalLogins.cshtmlTests.cs` | `OnGetLinkLoginCallbackAsync_AddLoginResult_UpdatesStatusMessageAndRedirects` |
| POST RemoveLogin — user not found | `Manage/ExternalLogins.cshtmlTests.cs` | `OnPostRemoveLoginAsync_UserNotFound_ReturnsNotFound` |
| POST RemoveLogin — fails | `Manage/ExternalLogins.cshtmlTests.cs` | `OnPostRemoveLoginAsync_RemoveLoginFails_SetsFailureMessageAndRedirects` |
| POST RemoveLogin — success | `Manage/ExternalLogins.cshtmlTests.cs` | `OnPostRemoveLoginAsync_RemoveLoginSucceeds_RefreshesSignInAndSetsSuccessMessage` |

---

## 12. Account Management — Personal Data

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    PersonalDataGET["GET /Account/Manage/PersonalData"]:::unitOnly

    DownloadPOST{"POST /Account/Manage/DownloadPersonalData\nuser found?"}

    DownloadNotFound["NotFoundObjectResult"]:::unitOnly

    DownloadSuccess["Returns JSON file download (user data, claims, logins, recovery codes)"]:::partial

    DeleteGET{"GET /Account/Manage/DeletePersonalData\nuser found?"}

    DeleteGetNotFound["NotFoundObjectResult"]:::unitOnly

    DeleteForm["Show confirmation form"]:::unitOnly

    DeletePOST{"POST /Account/Manage/DeletePersonalData\nstate check"}

    DeleteModelInvalid["Return page"]:::unitOnly

    DeletePostNotFound["NotFoundObjectResult"]:::unitOnly

    DeleteSuccess["Delete user, sign out, redirect"]:::covered

    PersonalDataGET -->|"download link"| DownloadPOST
    PersonalDataGET -->|"delete link"| DeleteGET
    DownloadPOST -->|"not found"| DownloadNotFound
    DownloadPOST -->|"found"| DownloadSuccess
    DeleteGET -->|"not found"| DeleteGetNotFound
    DeleteGET -->|"found"| DeleteForm
    DeleteForm --> DeletePOST
    DeletePOST -->|"invalid model"| DeleteModelInvalid
    DeleteModelInvalid --> DeleteForm
    DeletePOST -->|"not found"| DeletePostNotFound
    DeletePOST -->|"success"| DeleteSuccess
```

### Personal Data Tests

| Path | File | Test Method |
|---|---|---|
| GET /PersonalData — constructor | `Manage/PersonalData.cshtmlTests.cs` | `PersonalDataModel_WithValidDependencies_DoesNotThrowAndCreatesInstance` |
| GET /PersonalData — multiple instances | `Manage/PersonalData.cshtmlTests.cs` | `PersonalDataModel_WithDifferentLoggerInstances_CreatesDistinctInstances` |
| POST /DownloadPersonalData — not found | `Manage/DownloadPersonalData.cshtmlTests.cs` | `OnGet_UserNotFound_ReturnsNotFoundObjectResultWithMessage` |
| GET /DeletePersonalData — not found | `Manage/DeletePersonalData.cshtmlTests.cs` | `OnGet_UserNotFound_ReturnsNotFoundObjectResultWithMessage` |
| POST /DeletePersonalData — invalid model | `Manage/DeletePersonalData.cshtmlTests.cs` | `OnPostAsync_ModelStateInvalid_ReturnsPage` |
| POST /DeletePersonalData — not found | `Manage/DeletePersonalData.cshtmlTests.cs` | `OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult` |
| E2E: delete account, login fails | `AccountManagementTests.cs` (E2E) | `DeleteAccount_Success_SubsequentLoginFails` |

---

## 13. Minimal API — Passkey Endpoints

```mermaid
flowchart LR
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef noTest fill:#fee2e2,stroke:#dc2626

    subgraph CreationOpts["POST /Account/PasskeyCreationOptions"]
        CO_Guard["Null endpoint builder\nthrows ArgumentNullException"]:::unitOnly

        CO_Auth["Requires authenticated user\n+ antiforgery token"]:::noTest

        CO_NotFound["GetUserAsync returns null\n404"]:::unitOnly

        CO_Found["Build PasskeyUserEntity\nMakePasskeyCreationOptionsAsync\n200 JSON"]:::unitOnly
    end

    subgraph RequestOpts["POST /Account/PasskeyRequestOptions"]
        RO_Auth["Requires antiforgery token"]:::noTest

        RO_Null["username null or whitespace\nMakePasskeyRequestOptionsAsync(null)"]:::unitOnly

        RO_Found["username provided\nFindByNameAsync\nMakePasskeyRequestOptionsAsync(user)"]:::unitOnly
    end
```

---

## 14. Services

```mermaid
flowchart TD
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04

    subgraph GravatarSvc["GravatarService (IAvatarService)"]
        GV_Hash["SHA-256 hash email (lowercase)"]:::unitOnly

        GV_Found["Gravatar profile found\nreturn avatar URL"]:::unitOnly

        GV_NotFound["Gravatar profile not found (404)\nreturn null"]:::unitOnly

        GV_NullUrl["Profile found but no avatar URL\nreturn null"]:::unitOnly

        GV_OtherErr["Non-404 API exception\npropagates"]:::unitOnly

        GV_Cancel["Cancellation token\npropagates"]:::unitOnly
    end

    subgraph EmailSvc["EmailSender (IEmailSender via Resend)"]
        ES_Send["SendEmailAsync calls Resend API"]:::unitOnly

        ES_Throw["Resend throws\nexception propagates"]:::unitOnly

        ES_Ctor["Constructor variants"]:::unitOnly
    end

    GV_Hash --> GV_Found
    GV_Hash --> GV_NotFound
    GV_Hash --> GV_NullUrl
    GV_Hash --> GV_OtherErr
    GV_Hash --> GV_Cancel
```

### Service Tests

| Scenario | File | Test Method |
|---|---|---|
| Gravatar — profile found | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileFound_ReturnsAvatarUrl` |
| Gravatar — profile not found | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileNotFound_ReturnsNull` |
| Gravatar — null avatar URL | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_ProfileReturnsNullAvatarUrl_ReturnsNull` |
| Gravatar — non-404 exception | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_NonNotFoundApiException_PropagatesException` |
| Gravatar — SHA-256 hash casing | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_AlwaysHashesEmailToSha256Lowercase` |
| Gravatar — cancellation token | `GravatarServiceTests.cs` | `GetAvatarUrlAsync_PassesCancellationToken` |
| EmailSender — sends via Resend | `EmailSenderTests.cs` | `SendEmailAsync_VariousInputs_CallsResendWithExpectedMessage` |
| EmailSender — Resend throws | `EmailSenderTests.cs` | `SendEmailAsync_ResendThrows_PropagatesException` |
| EmailSender — constructor | `EmailSenderTests.cs` | `Constructor_ValidResend_CreatesInstance` |

---

## 15. Root & Utility Pages

```mermaid
flowchart TD
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    IndexPage["GET / (Home page)"]:::partial

    PrivacyPage["GET /Privacy"]:::partial

    ErrorPage["GET /Error"]:::unitOnly

    HealthEndpoint["GET /Health (DbContext health check)"]:::noTest

    AccessDeniedPage["GET /Account/AccessDenied"]:::partial

    IndexPage -.-> ErrorPage
    PrivacyPage -.-> ErrorPage
```

### Root & Utility Tests

| Path | File | Test Method |
|---|---|---|
| GET / — constructor | `Pages/Index.cshtmlTests.cs` | `Constructor_BothDependenciesNull_DoesNotThrowAndCreatesInstance` |
| GET /Error — no error ID | `Pages/Error.cshtmlTests.cs` | `OnGetAsync_NullOrWhitespaceErrorId_SkipsInteractionService` |
| GET /Error — with error ID | `Pages/Error.cshtmlTests.cs` | `OnGetAsync_ValidErrorId_CallsInteractionService` |
| GET /Error — with error message (logs) | `Pages/Error.cshtmlTests.cs` | `OnGetAsync_ValidErrorId_WithErrorMessage_LogsError` |
| GET /Error — ShowRequestId property | `Pages/Error.cshtmlTests.cs` | `ShowRequestId_VariousValues_ReturnsExpected` |

---

## 16. IdentityServer UI Pages

These pages implement the IdentityServer interactive UI — consent, grants, device flow, CIBA, server-side sessions, redirect, and diagnostics. All C# page models have unit tests; Consent, Grants, Diagnostics, and ServerSideSessions also have E2E tests.

### IdentityServer Page Tests

| Path | Unit Test File | E2E Test File | Coverage |
|---|---|---|---|
| `/Consent/Index` | `Pages/Consent/Index.cshtmlTests.cs` | `E2E/ConsentTests.cs` | ✅ Unit + E2E |
| `/Grants/Index` | `Pages/Grants/Index.cshtmlTests.cs` | `E2E/GrantsTests.cs` | ✅ Unit + E2E |
| `/Device/Index` | `Pages/Device/Index.cshtmlTests.cs` | — | 🟡 Unit only |
| `/Device/Success` | `Pages/Device/Success.cshtmlTests.cs` | — | 🟡 Unit only |
| `/Ciba/Index` | `Pages/Ciba/Index.cshtmlTests.cs` | — | 🟡 Unit only |
| `/ServerSideSessions/Index` | `Pages/ServerSideSessions/Index.cshtmlTests.cs` | `E2E/ServerSideSessionsTests.cs` | ✅ Unit + E2E |
| `/Redirect/Index` | `Pages/Redirect/Index.cshtmlTests.cs` | — | 🟡 Unit only |
| `/Diagnostics/Index` | `Pages/Diagnostics/Index.cshtmlTests.cs` | `E2E/DiagnosticsTests.cs` | ✅ Unit + E2E |

**E2E test helpers:**
- `Infrastructure/TestClientHelper.cs` — seeds a minimal OIDC client (`RequireConsent=true`, `authorization_code` grant, `openid` scope) and identity resources into `ApplicationDbContext` for use by `ConsentTests`

---

## 17. Coverage Summary Matrix

```mermaid
quadrantChart
    title Test Coverage by Path Category
    x-axis Low Path Count --> High Path Count
    y-axis Unit Only --> Unit + E2E
    quadrant-1 Well Tested
    quadrant-2 Needs E2E
    quadrant-3 Needs More Tests
    quadrant-4 Has E2E Gap

    Login Flow: [0.75, 0.85]
    Registration Flow: [0.60, 0.80]
    Password Recovery: [0.55, 0.70]
    Two-Factor Auth: [0.70, 0.65]
    Passkeys WebAuthn: [0.65, 0.30]
    External Login: [0.50, 0.20]
    Account Management: [0.80, 0.60]
    Personal Data: [0.45, 0.55]
    Email Change: [0.55, 0.25]
    Services: [0.40, 0.35]
    Root Pages: [0.25, 0.15]
    Minimal APIs: [0.45, 0.40]
```

### Path → Test Coverage Table

| Route | GET Unit | POST Unit | E2E | Notes |
|---|---|---|---|---|
| `/Account/Register` | ✅ | ✅ | ✅ | |
| `/Account/RegisterConfirmation` | 🔵 | — | ✅ | Constructor only |
| `/Account/ConfirmEmail` | ✅ | — | ✅ | |
| `/Account/ConfirmEmailChange` | ✅ | — | ✅ | Via EmailChangeTests |
| `/Account/ResendEmailConfirmation` | 🔵 | 🔵 | ✅ | Constructor only (unit); E2E via `ResendEmailConfirmation_NewLink_ConfirmsAccount` |
| `/Account/Login` | ✅ | ✅ | ✅ | |
| `/Account/LoginWith2fa` | ✅ | ✅ | ✅ | |
| `/Account/LoginWithRecoveryCode` | ✅ | ✅ | ✅ | |
| `/Account/Lockout` | 🔵 | — | ✅ | Constructor only |
| `/Account/Logout` | ✅ | ✅ | ✅ | GET + POST both sign out and redirect |
| `/Account/ForgotPassword` | 🔵 | ✅ | ✅ | |
| `/Account/ForgotPasswordConfirmation` | 🔵 | — | ✅ | Constructor only |
| `/Account/ResetPassword` | ✅ | ✅ | ✅ | |
| `/Account/ResetPasswordConfirmation` | 🔵 | — | ✅ | Constructor only |
| `/Account/ExternalLogin` | ✅ | ✅ | ❌ | No live OIDC in tests |
| `/Account/AccessDenied` | 🔵 | — | ❌ | Constructor only |
| `/Account/Manage/Index` | ✅ | ✅ | ✅ | Via AccountManagementTests |
| `/Account/Manage/Email` | ✅ | ✅ | ✅ | Via EmailChangeTests |
| `/Account/Manage/ChangePassword` | ✅ | ✅ | ✅ | |
| `/Account/Manage/SetPassword` | ✅ | ✅ | ❌ | |
| `/Account/Manage/TwoFactorAuthentication` | ✅ | — | ✅ | |
| `/Account/Manage/EnableAuthenticator` | ✅ | ✅ | ✅ | |
| `/Account/Manage/ShowRecoveryCodes` | ✅ | — | ✅ | |
| `/Account/Manage/GenerateRecoveryCodes` | ✅ | ✅ | ✅ | |
| `/Account/Manage/Disable2fa` | ✅ | ✅ | ✅ | Via Disable2faTests |
| `/Account/Manage/ResetAuthenticator` | ✅ | ✅ | ❌ | |
| `/Account/Manage/Passkeys` | ✅ | ⚠️ | ❌ | 3 tests Skipped |
| `/Account/Manage/RenamePasskey` | ✅ | ✅ | ❌ | |
| `/Account/Manage/ExternalLogins` | ✅ | ✅ | ❌ | |
| `/Account/Manage/PersonalData` | ✅ | — | ❌ | |
| `/Account/Manage/DownloadPersonalData` | ✅ | — | ❌ | |
| `/Account/Manage/DeletePersonalData` | ✅ | ✅ | ✅ | |
| `POST /Account/PasskeyCreationOptions` | — | ✅ | ❌ | Minimal API |
| `POST /Account/PasskeyRequestOptions` | — | ✅ | ❌ | Minimal API |
| `/Health` | ❌ | — | ❌ | Infrastructure endpoint |
| `/` | 🔵 | — | ❌ | Constructor only |
| `/Privacy` | 🔵 | — | ❌ | Constructor only |
| `/Error` | ✅ | — | ❌ | |
| `/Consent/Index` | ✅ | ✅ | ✅ | Allow, Deny, no-scopes E2E flows |
| `/Grants/Index` | ✅ | ✅ | ✅ | Page loads for authenticated user |
| `/Device/Index` | ✅ | ✅ | ❌ | |
| `/Device/Success` | ✅ | — | ❌ | Constructor only |
| `/Ciba/Index` | ✅ | — | ❌ | |
| `/ServerSideSessions/Index` | ✅ | ✅ | ✅ | Page loads after login |
| `/Redirect/Index` | ✅ | — | ❌ | |
| `/Diagnostics/Index` | ✅ | — | ✅ | Claims table visible after login |

### Coverage Gaps

The following paths have no meaningful behavioral test coverage and are candidates for improvement:

| Gap | Impact | Suggested Test |
|---|---|---|
| WebAuthn browser ceremony (create + sign) | High — core passkey flow untestable | Skip-marked tests in `Passkeys.cshtmlTests.cs` need completion |
| `POST /Account/PasskeyCreationOptions` — antiforgery rejection | Medium | Integration test with missing antiforgery header |
| `POST /Account/PasskeyRequestOptions` — antiforgery rejection | Medium | Integration test with missing antiforgery header |
| `/Account/ExternalLogin` — existing user sign-in success | Medium | Mock `ExternalLoginSignInAsync` success path |
| `/Account/ExternalLogin` — create user failure | Low | Mock `CreateAsync` failure result |
| `GET /Health` | Low | Simple `WebApplicationFactory` integration test |
| `POST /Account/Manage/DownloadPersonalData` — download content | Low | Verify JSON shape and data presence |
| `GET /Account/ResendEmailConfirmation` + `POST` | Low | Unit tests for OnGet/OnPost page model handlers (E2E already covered) |

---

## 18. Load, Property-Based & Resilience Tests

### Load Tests (`Identity.Tests/Load/`)

```bash
ASPNETCORE_ENVIRONMENT=Development SqlConnectionStringBuilder__InitialCatalog=IdentityTest dotnet test --project Identity.Tests --configuration Release -- --filter-trait "Category=Load"
```

Load tests use `Parallel.ForEachAsync` + `HttpClient` (self-signed cert ignored) against the real Kestrel server started by `PlaywrightFixture`. They are excluded from normal CI runs and only execute on `schedule` or `workflow_dispatch`.

> **Test parallelism note:** `xunit.runner.json` sets `parallelizeTestCollections: false`. This is required because `PlaywrightFixture` initializes `WebApplicationFactory<Program>`, which makes 8 concurrent Azure Key Vault calls at startup. When hundreds of unit tests run in parallel, thread pool saturation causes those async calls to time out and the factory throws "The entry point exited without ever building an IHost." Serializing collections eliminates the contention at the cost of a longer combined run (~5-6 min vs ~2.5 min). If you see this error, do not change the parallelism setting — diagnose the Azure credential or network path instead.

| Test | Endpoint | RPS | Pass Criterion |
|---|---|---|---|
| `DiscoveryEndpoint_Under50Rps_HasNegligibleFailures` | `/.well-known/openid-configuration` | ~50 | < 1% failure |
| `LoginPage_Under30Rps_HasNegligibleFailures` | `/Account/Login` | ~30 | < 2% failure |
| `JwksEndpoint_Under100Rps_HasNegligibleFailures` | `/.well-known/openid-configuration/jwks` | ~100 | < 1% failure |
| `HealthEndpoint_Under20Rps_AllSucceed` | `/Health` | ~20 | 0 failures |

### Property-Based Tests (`Identity.Tests/PropertyBased/`)

`[Trait("Category", "Unit")]` — run with the normal unit test suite.

| File | Focus |
|---|---|
| `PasswordHashingTests.cs` | Verifies `PasswordHasher<IdentityUser<Guid>>` round-trips any valid password (up to 72 bytes), rejects tampered hashes, and produces consistent results across instances |
| `InputSanitizationTests.cs` | Verifies Gravatar hash is lowercase hex for arbitrary email strings; verifies email sender passes through arbitrary subjects/bodies unchanged |

### Resilience Tests (`Identity.Tests/Resilience/`)

`[Trait("Category", "Unit")]` — run with the normal unit test suite.

| File | Focus |
|---|---|
| `ServiceResilienceTests.cs` | `EmailSender` propagates `ResendException`; `GravatarService` surfaces non-404 API exceptions; `SecretClient` propagates `RequestFailedException`; services tolerate `CancellationToken` cancellation |

---

## 19. Mutation Testing (Stryker)

Stryker.NET is configured in `stryker-config.json` with `mutation-level: Advanced`. It targets five core source files:

| File | Why it's targeted |
|---|---|
| `Identity.Api/EmailSender.cs` | Only production email path |
| `Identity.Api/GravatarService.cs` | Hash computation and error handling |
| `Identity.Api/Extensions/SecretClientExtensions.cs` | Key Vault secret fetch coordination |
| `Identity.Api/Extensions/ConfigurationExtensions.cs` | Startup config extraction |
| `Identity.Api/Extensions/EndpointRouteBuilderExtensions.cs` | Passkey endpoint registration |

**Thresholds:** high=80, low=60, break=50 (CI fails if mutation score < 50).

```bash
# Install once
dotnet tool install -g dotnet-stryker

# Run (slow — allow 10–30 minutes depending on machine)
dotnet stryker --config-file stryker-config.json
```

The CI `mutation` job runs on schedule (Monday 02:00 UTC) and on manual dispatch. Reports are uploaded as the `stryker-report` artifact (HTML + JSON).
