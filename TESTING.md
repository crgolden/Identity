# Testing Documentation

Maps every application path to the tests that cover it.

**Test types**
- **Unit** — xUnit page-model / service / API tests (`Category=Unit`) — 364 tests
- **E2E** — Playwright browser tests (`Category=E2E`)

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
16. [Coverage Summary Matrix](#16-coverage-summary-matrix)

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

    RegisterGET["GET /Account/Register\n────────────────────\n🟡 OnGetAsync_VariousReturnUrlValues\n🟡 OnGetAsync_ExternalSchemesReturned"]:::unitOnly

    RegisterPOST{"POST /Account/Register"}

    InvalidModel["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    CreateUser{"CreateUser\nsucceeds?"}

    RequireConfirm{"RequireConfirmed\nAccount?"}

    RegConfirm["GET /Account/RegisterConfirmation\n────────────────────\n🔵 Constructor_MultipleValidDependencies\n✅ E2E: Register_ConfirmEmail_Login_Succeeds"]:::covered

    EmailSent["EmailSender sends\nconfirmation link\n────────────────────\n🟡 Unit: EmailSenderTests\n✅ E2E: Register_ConfirmEmail_Login_Succeeds"]:::covered

    SignInDirect["Signed in directly\n(no confirm required)\n────────────────────\n🟡 OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount"]:::unitOnly

    ConfirmEmailGET["GET /Account/ConfirmEmail\n────────────────────\n🟡 OnGetAsync_NullOrWhitespaceUserIdOrCode\n🟡 ConfirmEmailModel_Constructor\n✅ E2E: Register_ConfirmEmail_Login_Succeeds"]:::covered

    ConfirmSuccess["Email confirmed\n➜ /Index\n────────────────────\n✅ E2E: Register_ConfirmEmail_Login_Succeeds"]:::covered

    ConfirmFail["Error message on page\n────────────────────\n🟡 OnGetAsync_NullOrWhitespaceUserIdOrCode_RedirectsToIndex"]:::unitOnly

    NullParams["Redirect to /Index\n────────────────────\n🟡 OnGetAsync_NullOrWhitespaceUserIdOrCode_RedirectsToIndex"]:::unitOnly

    ResendGET["GET /Account/ResendEmailConfirmation\n────────────────────\n🔵 Constructor test"]:::partial

    ResendPOST["POST /Account/ResendEmailConfirmation\n────────────────────\n🔵 Constructor test"]:::partial

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

---

## 3. Authentication — Login

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    LoginGET["GET /Account/Login\n────────────────────\n🟡 OnGetAsync_WithErrorMessage_AddsModelError\n🟡 OnGetAsync_WithoutErrorMessage_DoesNotAddModelError\n🟡 OnGetAsync_WithReturnUrl_SetsReturnUrl\n🟡 OnGetAsync_WithoutReturnUrl_DefaultsToRoot\n🟡 OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins"]:::unitOnly

    LoginPOST{"POST /Account/Login\nMethod?"}

    PasswordPath{"PasswordSignInAsync\nresult"}

    PasskeyPath["POST (passkey credential JSON)\n────────────────────\n🟡 OnPostAsync_PasskeySignIn_Succeeded_ReturnsLocalRedirect"]:::unitOnly

    ModelInvalid["Return page (no sign-in)\n────────────────────\n🟡 OnPostAsync_InvalidModelState_ReturnsPageWithoutSignIn"]:::unitOnly

    SignInSuccess["Redirect to returnUrl / /\n────────────────────\n✅ OnPostAsync_PasswordSignIn_Succeeded_ReturnsLocalRedirect\n✅ E2E: Login_ValidCredentials_Succeeds"]:::covered

    Requires2FA["Redirect to /Account/LoginWith2fa\n────────────────────\n✅ OnPostAsync_PasswordSignIn_RequiresTwoFactor_RedirectsToLoginWith2fa\n✅ E2E: TwoFactor_Setup_Login_WithTotpCode_Succeeds"]:::covered

    LockedOut["Redirect to /Account/Lockout\n────────────────────\n✅ OnPostAsync_PasswordSignIn_IsLockedOut_RedirectsToLockout\n✅ E2E: Login_FiveFailedAttempts_LocksAccount"]:::covered

    SignInFailed["Return page with error\n────────────────────\n✅ OnPostAsync_PasswordSignIn_Failed_ReturnsPageWithModelError\n✅ E2E: Login_WrongPassword_ShowsError"]:::covered

    LockoutPage["GET /Account/Lockout\n────────────────────\n🔵 Constructor test"]:::partial

    LoginWith2faPage["GET /Account/LoginWith2fa\n────────────────────\n🟡 OnGetAsync_TwoFactorUserExists_SetsReturnUrlAndReturnsPage\n🟡 OnGetAsync_UserIsNull_ThrowsInvalidOperationException"]:::unitOnly

    LoginWith2faPOST{"POST /Account/LoginWith2fa\nresult"}

    TwoFASuccess["Redirect authenticated\n────────────────────\n✅ OnPostAsync_Succeeds_RedirectsAndSetsStatusMessageAndLogs\n✅ E2E: TwoFactor_Setup_Login_WithTotpCode_Succeeds"]:::covered

    TwoFAInvalid["Return page with error\n────────────────────\n🟡 OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage"]:::unitOnly

    TwoFANoUser["Throws InvalidOperationException\n────────────────────\n🟡 OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException"]:::unitOnly

    RecoveryLink["Link to /Account/LoginWithRecoveryCode"]

    RecoveryGET["GET /Account/LoginWithRecoveryCode\n────────────────────\n🟡 OnGetAsync_ValidUser_SetsPropertiesAndReturnsPageResult\n🟡 LoginWithRecoveryCodeModel_Constructor_AllNulls_DoesNotThrowAndDefaults"]:::unitOnly

    RecoveryPOST{"POST /Account/LoginWithRecoveryCode\nresult"}

    RecoverySuccess["Redirect authenticated\n────────────────────\n✅ E2E: TwoFactor_Login_WithRecoveryCode_Succeeds"]:::covered

    RecoveryInvalid["Return page with error\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPageResult"]:::unitOnly

    LogoutPOST["POST /Account/Logout\n────────────────────\n🟡 Constructor_NullSignInManager_DoesNotThrow"]:::unitOnly

    SignedOut["Redirect to /Index"]

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
    SignInSuccess --> LogoutPOST
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

    ForgotGET["GET /Account/ForgotPassword\n────────────────────\n🟡 ForgotPasswordModel_Constructor_DependencyNullAllowed"]:::unitOnly

    ForgotPOST{"POST /Account/ForgotPassword"}

    ForgotInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    ForgotNoUser["Redirect to Confirmation\n(no email sent — security)\n────────────────────\n🟡 OnPostAsync_UserNullOrUnconfirmed_RedirectsToConfirmation_DoesNotSendEmail"]:::unitOnly

    ForgotSendEmail["Generate token\nSend reset link\n────────────────────\n✅ E2E: ForgotPassword_Reset_LoginWithNewPassword_Succeeds"]:::covered

    ForgotConfirmPage["GET /Account/ForgotPasswordConfirmation\n────────────────────\n🔵 Constructor test"]:::partial

    ResetGET{"GET /Account/ResetPassword\ncode present?"}

    ResetNullCode["Returns BadRequest\n────────────────────\n🟡 OnGet_CodeIsNull_ReturnsBadRequestWithMessage"]:::unitOnly

    ResetMalformed["Throws FormatException\n────────────────────\n🟡 OnGet_MalformedCode_ThrowsFormatException"]:::unitOnly

    ResetForm["Show reset form\n────────────────────\n🟡 OnGet_ValidBase64UrlEncodedCode_SetsInputCodeAndReturnsPage\n🟡 ResetPasswordModel_ValidUserManager_ConstructsSuccessfully"]:::unitOnly

    ResetPOST{"POST /Account/ResetPassword"}

    ResetInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    ResetNoUser["Redirect to Confirmation\n(no error revealed)\n────────────────────\n🟡 OnPostAsync_UserMissingOrResetSucceeds_RedirectsToConfirmation"]:::unitOnly

    ResetFails["Return page with errors\n────────────────────\n🟡 OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage"]:::unitOnly

    ResetSuccess["Redirect to /Account/ResetPasswordConfirmation\n────────────────────\n🟡 OnPostAsync_UserMissingOrResetSucceeds_RedirectsToConfirmation\n✅ E2E: ForgotPassword_Reset_LoginWithNewPassword_Succeeds"]:::covered

    ResetConfirmPage["GET /Account/ResetPasswordConfirmation\n────────────────────\n🔵 Constructor test"]:::partial

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

    TwoFAPage["GET /Account/Manage/TwoFactorAuthentication\n────────────────────\n🟡 OnGetAsync_UserFound_SetsPropertiesAndReturnsPageResult\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    EnableAuthGET{"GET /Account/Manage/EnableAuthenticator\nuser found?"}

    EnableNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage"]:::unitOnly

    EnableForm["Show QR code + shared key\n────────────────────\n🟡 Constructor_WithValidDependencies_CreatesInstance\n✅ E2E: TwoFactor_Setup_Login_WithTotpCode_Succeeds"]:::covered

    EnablePOST{"POST /Account/Manage/EnableAuthenticator"}

    EnableUserNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    EnableInvalid["Return page with error\n────────────────────\n🟡 OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage"]:::unitOnly

    EnableSuccess["Redirect to ShowRecoveryCodes\n(or TwoFactorAuthentication if no codes)\n────────────────────\n🟡 OnPostAsync_ValidToken_RedirectsBasedOnRecoveryCodesCount\n✅ E2E: TwoFactor_Setup_Login_WithTotpCode_Succeeds"]:::covered

    ShowCodes{"GET /Account/Manage/ShowRecoveryCodes\ncodes present?"}

    ShowCodesEmpty["Redirect to /Account/Manage/TwoFactorAuthentication\n────────────────────\n🟡 OnGet_RecoveryCodesNullOrEmpty_RedirectsToTwoFactorAuthentication"]:::unitOnly

    ShowCodesPage["Display recovery codes\n────────────────────\n🟡 OnGet_RecoveryCodesHasItems_ReturnsPageResult\n✅ E2E: TwoFactor_Setup_Login_WithTotpCode_Succeeds"]:::covered

    GenCodesGET{"GET /Account/Manage/GenerateRecoveryCodes\nstate check"}

    GenCodesNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage"]:::unitOnly

    Gen2faDisabled["Throws InvalidOperationException\n────────────────────\n🟡 OnPostAsync_TwoFactorDisabled_ThrowsInvalidOperationException"]:::unitOnly

    GenCodesPOST["POST — generate new codes\n────────────────────\n🟡 OnPostAsync_TwoFactorEnabled_GeneratesCodesAndRedirects\n✅ E2E: TwoFactor_Login_WithRecoveryCode_Succeeds"]:::covered

    Disable2faGET{"GET /Account/Manage/Disable2fa\nstate check"}

    DisableNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGet_UserIsNull_ReturnsNotFoundWithUserIdInMessage"]:::unitOnly

    Disable2faNotEnabled["Throws InvalidOperationException\n────────────────────\n🟡 OnGet_TwoFactorState_BehavesAsExpected"]:::unitOnly

    Disable2faForm["Show confirmation form\n────────────────────\n🟡 OnGet_TwoFactorState_BehavesAsExpected"]:::unitOnly

    Disable2faPOST{"POST /Account/Manage/Disable2fa"}

    DisableFails["Throws InvalidOperationException\n────────────────────\n🟡 OnPostAsync_DisableFails_ThrowsInvalidOperationException"]:::unitOnly

    DisableSuccess["Redirect to /Account/Manage/TwoFactorAuthentication\n────────────────────\n🟡 OnPostAsync_Succeeds_RedirectsAndSetsStatusMessageAndLogs"]:::unitOnly

    ResetAuthGET["GET /Account/Manage/ResetAuthenticator\n────────────────────\n🟡 OnGet_UserExistence_ReturnsExpectedResult\n🟡 ResetAuthenticatorModel_Constructor_WithValidDependencies_DoesNotThrow"]:::unitOnly

    ResetAuthPOST["POST — reset authenticator\n────────────────────\n🟡 OnPostAsync_UserExists_ResetsAndRedirectsRegardlessOfIdentityResult\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage"]:::unitOnly

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

---

## 6. Passkeys (WebAuthn)

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c
    classDef noTest fill:#fee2e2,stroke:#dc2626

    PasskeysGET{"GET /Account/Manage/Passkeys\nuser found?"}

    PasskeysNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    PasskeysList["Show passkey list\n────────────────────\n🟡 PasskeysModel_Ctor_ValidManagers_PropertiesInitializedToNull"]:::unitOnly

    CreationOptsEndpoint{"POST /Account/PasskeyCreationOptions\n(Minimal API)\nuser found?"}

    CreationOpts404["404 Not Found\n────────────────────\n🟡 PasskeyCreationOptions_UserNotFound_Returns404"]:::unitOnly

    CreationOpts200["200 JSON creation options\n────────────────────\n🟡 PasskeyCreationOptions_UserFound_ReturnsOkWithJson\n🟡 PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager"]:::unitOnly

    WebAuthnCeremony["Browser performs\nWebAuthn creation ceremony\n────────────────────\n❌ No automated test"]:::noTest

    AddPasskeyPOST{"POST /Account/Manage/Passkeys\n(AddPasskey handler)\nuser found?"}

    AddNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostAddPasskeyAsync_UserNotFound_ReturnsNotFound"]:::unitOnly

    AttestFails["Redirect with failure message\n────────────────────\n⚠️ OnPostAddPasskeyAsync_AttestationFails_RedirectsWithFailureMessage_Partial\n(test marked Skip)"]:::partial

    AddOrUpdateFails["Redirect with failure message\n────────────────────\n⚠️ OnPostAddPasskeyAsync_AddOrUpdateFails_RedirectsWithFailureMessage_Partial\n(test marked Skip)"]:::partial

    AddSuccess["Redirect to /Account/Manage/RenamePasskey\n────────────────────\n⚠️ OnPostAddPasskeyAsync_Success_RedirectsToRenamePasskey_Partial\n(test marked Skip)"]:::partial

    RenameGET{"GET /Account/Manage/RenamePasskey?id\nstate check"}

    RenameInvalidB64["Redirect to Passkeys + status\n────────────────────\n🟡 OnGetAsync_InvalidBase64Id_RedirectsToPasskeysAndSetsStatusMessage"]:::unitOnly

    RenameNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_PasskeyNotFound_ReturnsNotFoundWithMessage"]:::unitOnly

    RenameForm["Show rename form\n────────────────────\n🟡 Constructor_ValidDependencies_CreatesInstance"]:::unitOnly

    RenamePOST["POST — update passkey name\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage"]:::unitOnly

    UpdatePasskeyPOST["POST /Account/Manage/Passkeys\n(UpdatePasskey handler)\n────────────────────\n🟡 OnPostUpdatePasskeyAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    RequestOptsEndpoint{"POST /Account/PasskeyRequestOptions\n(Minimal API, during login)\nusername provided?"}

    RequestOptsNull["Options with null user\n────────────────────\n🟡 PasskeyRequestOptions_NullUsername_MakesRequestOptionsWithNullUser\n🟡 PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser"]:::unitOnly

    RequestOptsUser["Find user, options with user entity\n────────────────────\n🟡 PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions\n🟡 PasskeyRequestOptions_ReturnsOkWithJson"]:::unitOnly

    PasskeySignIn["POST /Account/Login\n(passkey credential JSON)\n────────────────────\n🟡 OnPostAsync_PasskeySignIn_Succeeded_ReturnsLocalRedirect"]:::unitOnly

    NullCheck["MapAdditionalIdentityEndpoints\nnull endpoint builder\n────────────────────\n🟡 MapAdditionalIdentityEndpoints_NullEndpoints_ThrowsArgumentNullException"]:::unitOnly

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
| `MapAdditionalIdentityEndpoints` null guard | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `MapAdditionalIdentityEndpoints_NullEndpoints_ThrowsArgumentNullException` |
| POST /PasskeyCreationOptions — user not found | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserNotFound_Returns404` |
| POST /PasskeyCreationOptions — 200 + JSON | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserFound_ReturnsOkWithJson` |
| POST /PasskeyCreationOptions — user entity | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager` |
| POST /PasskeyRequestOptions — null username | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_NullUsername_MakesRequestOptionsWithNullUser` |
| POST /PasskeyRequestOptions — whitespace | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser` |
| POST /PasskeyRequestOptions — with username | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions` |
| POST /PasskeyRequestOptions — 200 | `PasskeyEndpointRouteBuilderExtensionsTests.cs` | `PasskeyRequestOptions_ReturnsOkWithJson` |
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

    LoginPage["GET /Account/Login\n(Google button visible)\n────────────────────\n🟡 OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins"]:::unitOnly

    GoogleChallenge["Challenge Google OIDC provider\n(browser redirect to Google)\n────────────────────\n❌ No automated test"]:::noTest

    GoogleCallback{"GET /Account/ExternalLogin\n?callback\nremote error?"}

    RemoteError["Redirect to /Account/Login\nwith error message\n────────────────────\n🟡 OnGetCallbackAsync_RemoteErrorProvided_SetsErrorMessageAndRedirectsToLogin"]:::unitOnly

    InfoNull["Redirect to /Account/Login\nwith error message\n────────────────────\n🟡 OnGetCallbackAsync_InfoIsNull_SetsErrorMessageAndRedirectsToLogin"]:::unitOnly

    ExistingUserSignIn["Existing user found\n→ Sign in directly\n────────────────────\n❌ No unit test for success path"]:::noTest

    ConfirmationForm{"POST /Account/ExternalLogin\n/Confirmation\nstate check"}

    ConfirmModelInvalid["Return page\n────────────────────\n🟡 OnPostConfirmationAsync_ModelStateInvalid_ReturnsPageAndSetsProviderDisplayNameAndReturnUrl"]:::unitOnly

    ConfirmInfoNull["Redirect to Login with error\n────────────────────\n🟡 OnPostConfirmationAsync_InfoNull_ReturnsRedirectToLoginAndSetsErrorMessage"]:::unitOnly

    CreateAndAddLogin{"Create user\n+ AddLogin\nsucceeds?"}

    ConfirmSuccess["Redirect (based on RequireConfirmedAccount)\n────────────────────\n🟡 OnPostConfirmationAsync_CreateAndAddLogin_Succeeds_ConditionalRedirect"]:::unitOnly

    ConfirmFail["Return page with errors\n────────────────────\n❌ No test for create failure path"]:::noTest

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

    ProfileNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    ProfilePage["Show profile form\n(username, phone, Gravatar avatar)\n────────────────────\n🟡 OnGetAsync_UserExists_LoadsUsernameAndPhoneAndReturnsPage\n🟡 Constructor_ValidDependencies_DoesNotThrow"]:::unitOnly

    ProfilePOST{"POST /Account/Manage/Index\nstate check"}

    ProfileUserNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage"]:::unitOnly

    ProfileModelInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPageAndDoesNotChangePhoneOrSignIn"]:::unitOnly

    ProfileSaved["Update phone → RefreshSignIn\n────────────────────\n✅ E2E: ChangePassword_Success_OldPasswordNoLongerWorks\n(visits profile page)"]:::covered

    GravatarService["GravatarService.GetAvatarUrlAsync()\n────────────────────\n🟡 GetAvatarUrlAsync_ProfileFound_ReturnsAvatarUrl\n🟡 GetAvatarUrlAsync_ProfileNotFound_ReturnsNull\n🟡 GetAvatarUrlAsync_ProfileReturnsNullAvatarUrl_ReturnsNull\n🟡 GetAvatarUrlAsync_NonNotFoundApiException_PropagatesException\n🟡 GetAvatarUrlAsync_AlwaysHashesEmailToSha256Lowercase\n🟡 GetAvatarUrlAsync_PassesCancellationToken"]:::unitOnly

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

    EmailNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult\n(via EmailModel_Constructor tests)"]:::unitOnly

    EmailPage["Show current email + change form\n────────────────────\n🟡 Constructor_ValidDependencies_InitializesDefaults\n🟡 Constructor_MultipleInstances_AreIndependent"]:::unitOnly

    SendVerifPOST{"POST (SendVerificationEmail handler)\nstate check"}

    SendVerifInvalid["Return page\n────────────────────\n🟡 OnPostSendVerificationEmailAsync_InvalidModelState_ReturnsPage"]:::unitOnly

    SendVerifNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId"]:::unitOnly

    SendVerifSuccess["Send email → Redirect\n────────────────────\n🟡 OnPostSendVerificationEmailAsync_ValidUser_SendsEmailAndRedirects"]:::unitOnly

    ChangeEmailPOST["POST (ChangeEmail handler)\n────────────────────\n🟡 OnPostChangeEmailAsync_UserNotFound_ReturnsNotFound"]:::unitOnly

    ConfirmEmailChangeGET{"GET /Account/ConfirmEmailChange\nparams valid?"}

    ConfirmNull["Redirect to /Account/Manage/Index\n────────────────────\n🟡 OnGetAsync_NullParameters_RedirectsToIndex"]:::unitOnly

    ConfirmChangeFails["Return page with error\n────────────────────\n🟡 OnGetAsync_ChangeEmailFails_ReturnsPageAndSetsStatusMessage"]:::unitOnly

    ConfirmChangeSuccess["Change email → RefreshSignIn\n────────────────────\n🟡 OnGetAsync_AllOperationsSucceed_RefreshesSignInAndSetsSuccessMessage"]:::unitOnly

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
    ConfirmEmailChangeGET -->|"change fails"| ConfirmChangeFails
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

---

## 10. Account Management — Password

```mermaid
flowchart TD
    classDef covered fill:#bbf7d0,stroke:#15803d
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04
    classDef partial fill:#fed7aa,stroke:#ea580c

    ChangePwdGET{"GET /Account/Manage/ChangePassword\nuser found?"}

    ChangePwdNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    ChangePwdForm["Show change password form\n────────────────────\n🟡 Constructor_WithValidDependencies_DoesNotThrow"]:::unitOnly

    ChangePwdPOST{"POST /Account/Manage/ChangePassword\nstate check"}

    ChangePwdInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    ChangePwdUserNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundWithUserId"]:::unitOnly

    ChangePwdSuccess["RefreshSignIn → Redirect\n────────────────────\n✅ E2E: ChangePassword_Success_OldPasswordNoLongerWorks"]:::covered

    SetPwdGET{"GET /Account/Manage/SetPassword\nstate check"}

    SetPwdNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdInMessage"]:::unitOnly

    SetPwdHasPwd["Redirect to ChangePassword\n────────────────────\n🟡 OnGetAsync_ExistingUser_BehavesBasedOnHasPassword"]:::unitOnly

    SetPwdForm["Show set password form\n────────────────────\n🟡 OnGetAsync_ExistingUser_BehavesBasedOnHasPassword"]:::unitOnly

    SetPwdPOST{"POST /Account/Manage/SetPassword\nstate check"}

    SetPwdInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    SetPwdNotFoundPost["NotFoundObjectResult\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage"]:::unitOnly

    SetPwdSuccess["Set password → RefreshSignIn\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

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

    ExtLoginsNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage"]:::unitOnly

    ExtLoginsPage["Show linked / available providers\n────────────────────\n🟡 ExternalLoginsModel_Constructor_AllParametersNull_DoesNotThrowCreatesInstance\n🟡 ExternalLoginsModel_Constructor_NullAndMockedUserStore_ObjectConstructedAndDefaults"]:::unitOnly

    LinkLoginPOST["POST LinkLogin handler\n→ Challenge provider\n────────────────────\n🟡 OnPostLinkLoginAsync_Provider_ReturnsChallengeAndSignsOut"]:::unitOnly

    GoogleRedirect["Browser redirects to Google\n────────────────────\n❌ No automated test"]:::noTest

    LinkCallbackGET{"GET LinkLoginCallback\nstate check"}

    CallbackNoInfo["Throws InvalidOperationException\n────────────────────\n🟡 OnGetLinkLoginCallbackAsync_NoExternalLoginInfo_ThrowsInvalidOperationException"]:::unitOnly

    CallbackNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGetLinkLoginCallbackAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    CallbackAddResult["Update status message → Redirect\n────────────────────\n🟡 OnGetLinkLoginCallbackAsync_AddLoginResult_UpdatesStatusMessageAndRedirects"]:::unitOnly

    RemoveLoginPOST{"POST RemoveLogin handler\nuser found?"}

    RemoveNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostRemoveLoginAsync_UserNotFound_ReturnsNotFound"]:::unitOnly

    RemoveFails["Set failure message → Redirect\n────────────────────\n🟡 OnPostRemoveLoginAsync_RemoveLoginFails_SetsFailureMessageAndRedirects"]:::unitOnly

    RemoveSuccess["RefreshSignIn → set success message\n────────────────────\n🟡 OnPostRemoveLoginAsync_RemoveLoginSucceeds_RefreshesSignInAndSetsSuccessMessage"]:::unitOnly

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

    PersonalDataGET["GET /Account/Manage/PersonalData\n────────────────────\n🟡 PersonalDataModel_WithValidDependencies_DoesNotThrowAndCreatesInstance\n🟡 PersonalDataModel_WithDifferentLoggerInstances_CreatesDistinctInstances"]:::unitOnly

    DownloadPOST{"POST /Account/Manage/DownloadPersonalData\nuser found?"}

    DownloadNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGet_UserNotFound_ReturnsNotFoundObjectResultWithMessage"]:::unitOnly

    DownloadSuccess["Returns JSON file download\n(user data, claims, logins, recovery codes)\n────────────────────\n🔵 Constructor_WithValidDependencies_InstanceCreatedAndOnGetReturnsNotFound"]:::partial

    DeleteGET{"GET /Account/Manage/DeletePersonalData\nuser found?"}

    DeleteGetNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnGet_UserNotFound_ReturnsNotFoundObjectResultWithMessage"]:::unitOnly

    DeleteForm["Show confirmation form\n────────────────────\n🟡 Constructor_ValidDependencies_InitializesDefaults"]:::unitOnly

    DeletePOST{"POST /Account/Manage/DeletePersonalData\nstate check"}

    DeleteModelInvalid["Return page\n────────────────────\n🟡 OnPostAsync_ModelStateInvalid_ReturnsPage"]:::unitOnly

    DeletePostNotFound["NotFoundObjectResult\n────────────────────\n🟡 OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult"]:::unitOnly

    DeleteSuccess["Delete user → Sign out → Redirect\n────────────────────\n✅ E2E: DeleteAccount_Success_SubsequentLoginFails"]:::covered

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
        CO_Guard["Null endpoint builder\n→ ArgumentNullException\n────────────────────\n🟡 MapAdditionalIdentityEndpoints_NullEndpoints_ThrowsArgumentNullException"]:::unitOnly

        CO_Auth["Requires authenticated user\n+ antiforgery token\n────────────────────\n❌ No integration test for auth rejection"]:::noTest

        CO_NotFound["GetUserAsync returns null\n→ 404\n────────────────────\n🟡 PasskeyCreationOptions_UserNotFound_Returns404"]:::unitOnly

        CO_Found["Build PasskeyUserEntity\n→ MakePasskeyCreationOptionsAsync\n→ 200 JSON\n────────────────────\n🟡 PasskeyCreationOptions_UserFound_ReturnsOkWithJson\n🟡 PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager"]:::unitOnly
    end

    subgraph RequestOpts["POST /Account/PasskeyRequestOptions"]
        RO_Auth["Requires antiforgery token\n────────────────────\n❌ No integration test for auth rejection"]:::noTest

        RO_Null["username null / whitespace\n→ MakePasskeyRequestOptionsAsync(null)\n────────────────────\n🟡 PasskeyRequestOptions_NullUsername_MakesRequestOptionsWithNullUser\n🟡 PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser"]:::unitOnly

        RO_Found["username provided\n→ FindByNameAsync(username)\n→ MakePasskeyRequestOptionsAsync(user)\n────────────────────\n🟡 PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions\n🟡 PasskeyRequestOptions_ReturnsOkWithJson"]:::unitOnly
    end
```

---

## 14. Services

```mermaid
flowchart TD
    classDef unitOnly fill:#fef9c3,stroke:#ca8a04

    subgraph GravatarSvc["GravatarService (IAvatarService)"]
        GV_Hash["SHA-256 hash email\n(lowercase)\n────────────────────\n🟡 GetAvatarUrlAsync_AlwaysHashesEmailToSha256Lowercase"]:::unitOnly

        GV_Found["Gravatar profile found\n→ return avatar URL\n────────────────────\n🟡 GetAvatarUrlAsync_ProfileFound_ReturnsAvatarUrl"]:::unitOnly

        GV_NotFound["Gravatar profile not found (404)\n→ return null\n────────────────────\n🟡 GetAvatarUrlAsync_ProfileNotFound_ReturnsNull"]:::unitOnly

        GV_NullUrl["Profile found but no avatar URL\n→ return null\n────────────────────\n🟡 GetAvatarUrlAsync_ProfileReturnsNullAvatarUrl_ReturnsNull"]:::unitOnly

        GV_OtherErr["Non-404 API exception\n→ propagates\n────────────────────\n🟡 GetAvatarUrlAsync_NonNotFoundApiException_PropagatesException"]:::unitOnly

        GV_Cancel["Cancellation token\n→ propagates\n────────────────────\n🟡 GetAvatarUrlAsync_PassesCancellationToken"]:::unitOnly
    end

    subgraph EmailSvc["EmailSender (IEmailSender via Resend)"]
        ES_Send["SendEmailAsync → Resend API\n────────────────────\n🟡 SendEmailAsync_VariousInputs_CallsResendWithExpectedMessage"]:::unitOnly

        ES_Throw["Resend throws\n→ exception propagates\n────────────────────\n🟡 SendEmailAsync_ResendThrows_PropagatesException"]:::unitOnly

        ES_Ctor["Constructor variants\n────────────────────\n🟡 Constructor_ValidResend_CreatesInstance\n🟡 Constructor_DifferentResendInstances_CreatesDistinctInstances"]:::unitOnly
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

    IndexPage["GET /\n(Home page)\n────────────────────\n🔵 Constructor_BothDependenciesNull_DoesNotThrowAndCreatesInstance"]:::partial

    PrivacyPage["GET /Privacy\n────────────────────\n🔵 Constructor test"]:::partial

    ErrorPage["GET /Error\n────────────────────\n🟡 Constructor_ValidInteractionService_InitializesDefaults\n🟡 OnGetAsync_NullOrWhitespaceErrorId_SkipsInteractionService\n🟡 OnGetAsync_ValidErrorId_CallsInteractionService\n🟡 OnGetAsync_ValidErrorId_WithErrorMessage_LogsError\n🟡 ShowRequestId_VariousValues_ReturnsExpected"]:::unitOnly

    HealthEndpoint["GET /Health\n(DbContext health check)\n────────────────────\n❌ No unit test\n❌ No E2E test"]:::noTest

    AccessDeniedPage["GET /Account/AccessDenied\n────────────────────\n🔵 Constructor test"]:::partial

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

## 16. Coverage Summary Matrix

```mermaid
quadrantChart
    title Test Coverage by Path Category
    x-axis "Low Path Count" --> "High Path Count"
    y-axis "Unit Only" --> "Unit + E2E"
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
| `/Account/ConfirmEmailChange` | ✅ | — | ❌ | |
| `/Account/ResendEmailConfirmation` | 🔵 | 🔵 | ❌ | Constructor only |
| `/Account/Login` | ✅ | ✅ | ✅ | |
| `/Account/LoginWith2fa` | ✅ | ✅ | ✅ | |
| `/Account/LoginWithRecoveryCode` | ✅ | ✅ | ✅ | |
| `/Account/Lockout` | 🔵 | — | ✅ | Constructor only |
| `/Account/Logout` | — | 🔵 | ❌ | Constructor only |
| `/Account/ForgotPassword` | 🔵 | ✅ | ✅ | |
| `/Account/ForgotPasswordConfirmation` | 🔵 | — | ✅ | Constructor only |
| `/Account/ResetPassword` | ✅ | ✅ | ✅ | |
| `/Account/ResetPasswordConfirmation` | 🔵 | — | ✅ | Constructor only |
| `/Account/ExternalLogin` | ✅ | ✅ | ❌ | No live OIDC in tests |
| `/Account/AccessDenied` | 🔵 | — | ❌ | Constructor only |
| `/Account/Manage/Index` | ✅ | ✅ | ✅ | Via AccountManagementTests |
| `/Account/Manage/Email` | ✅ | ✅ | ❌ | |
| `/Account/ConfirmEmailChange` | ✅ | — | ❌ | |
| `/Account/Manage/ChangePassword` | ✅ | ✅ | ✅ | |
| `/Account/Manage/SetPassword` | ✅ | ✅ | ❌ | |
| `/Account/Manage/TwoFactorAuthentication` | ✅ | — | ✅ | |
| `/Account/Manage/EnableAuthenticator` | ✅ | ✅ | ✅ | |
| `/Account/Manage/ShowRecoveryCodes` | ✅ | — | ✅ | |
| `/Account/Manage/GenerateRecoveryCodes` | ✅ | ✅ | ✅ | |
| `/Account/Manage/Disable2fa` | ✅ | ✅ | ❌ | |
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
| `/Account/Logout` — actual sign-out behavior | Low | E2E: verify session cookie cleared |
| `POST /Account/Manage/DownloadPersonalData` — download content | Low | Verify JSON shape and data presence |
| `GET /Account/ResendEmailConfirmation` + `POST` | Low | Unit tests for OnGet/OnPost handlers |
