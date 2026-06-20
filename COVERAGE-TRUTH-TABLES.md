# Identity: Coverage Truth Tables

Per-method decision tables built per the workspace standard in
[../TESTING-COVERAGE.md](../TESTING-COVERAGE.md), which applies the unit, the three laws, and the one
legend defined in [../DESIGN-LANGUAGE.md](../DESIGN-LANGUAGE.md). Each row is one unit; its status
(`✅ won / ❌ open / ⬆️ escalated / ⏳ parked`) and home level (Unit / E2E / Smoke / Manual) are read
from that one legend, not redefined here.

**Status of this file:** page-model coverage complete (2026-06-12). Covers `ConsentModel` (the
highest-complexity method, chosen to stress the methodology), `PasskeysModel` (36 uncovered branches),
`DeviceModel` (29) — the top three by uncovered-branch count from the 2026-06-12 baseline (Identity unit
= 60.8% branch / 66.2% blended) — and the remaining account/manage page-model handlers `EmailModel`,
`RenamePasskeyModel`, `DeletePersonalDataModel`, `ExternalLoginModel` (not individually quantified in
the baseline; counts below are MC/DC `tests_to_author` read from source).

---

## Pilot project: `Identity` (page models)

### Class: `ConsentModel`
`Identity/Identity/Pages/Account/Manage/Consent.cshtml.cs`
Unit tests: `Identity.Tests/Pages/Account/Manage/Consent.cshtmlTests.cs`
E2E tests: `Identity.Tests/E2E/ConsentTests.cs`

Tally: **rows 19 | ✅ 4 | ❌ 11 | ⏳ 4** &nbsp;·&nbsp; level split: **Unit 17 | E2E 0(sole) | Smoke 0 | Manual 0**
(E2E exists for 3 paths but does not discharge their Unit obligation; see notes.)

---

#### `ConsentModel(IIdentityServerInteractionService, IEventService)`
Straight-line, no control flow. `tests_to_author = 1`.

| # | Path | Outcome | Reachable? | Level | Status |
|---|---|---|---|---|---|
| 1 | Construct | instance, no throw | yes | Unit | ✅ `Constructor_NullParameters_DoesNotThrow` |

---

#### `OnGetAsync(string? returnUrl)`
Decisions: **A** = `!await SetViewModelAsync(returnUrl)`. `tests_to_author = 1 + (2−1) = 2`.

| # | Path | A (SetViewModel) | Outcome | Reachable? | Level | Status |
|---|---|---|---|---|---|---|
| 1 | Happy | returns true | `Input` set, `Page()` | yes | Unit | ❌ Gap. Needs a constructable `AuthorizationRequest` (proven constructable: `// public for testing`). E2E walks it but does not discharge this. |
| 2 | Flip A | returns false | `RedirectToPage("/Error")` | yes | Unit | ✅ `OnGetAsync_NullReturnUrl_RedirectsToError`, `OnGetAsync_ValidReturnUrl_InteractionReturnsNull_RedirectsToError` |

---

#### `SetViewModelAsync(string? returnUrl)` (private; exercised via OnGet/OnPost)
Decisions: **B** = `IsNullOrWhiteSpace(returnUrl)`, **C** = `request != null` (only when B false).
`tests_to_author = 1 + (2−1) + (2−1) = 3`.

| # | Path | B | C | Outcome | Reachable? | Level | Status |
|---|---|---|---|---|---|---|---|
| 1 | Blank url | T | — | `false` | yes | Unit | ✅ via `OnGetAsync_NullReturnUrl_RedirectsToError` |
| 2 | No context | F | F | `false` | yes | Unit | ✅ via `OnGetAsync_ValidReturnUrl_InteractionReturnsNull_RedirectsToError` |
| 3 | Happy | F | T | `View` set, `true` | yes | Unit | ❌ Gap. Constructable `AuthorizationRequest` required. |
| 4 | C with B true | T | T/F | n/a | ⏳ no | Unit | C not evaluated when B true. Reachable only if the `IsNullOrWhiteSpace` early-return is removed. Retained. |

---

#### `OnPostAsync()`  — the stress method
Decisions: **D** = `request == null`; **E** = three-way `Button` (`"no"` / `"yes"` / other);
**F** = `ScopesConsented.Count != 0` (under yes); **G** = `!ConsentOptions.EnableOfflineAccess`
(under yes+scopes); **H** = `grantedConsent != null`; **I** = `ThrowIfNull(Input.ReturnUrl)` guard
(under H true); **J** = `!await SetViewModelAsync(Input.ReturnUrl)` (under H false).

| # | Path | D | E | F | G | H | I/J | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Happy: grant | F | yes | T | offline-on | T | I pass | `GrantConsentAsync` + `Redirect` | yes | Unit | ❌ Gap. Pure logic over `_interaction`/`_events` mocks + constructable request. **E2E `Consent_Allow_RedirectsWithCode` covers it but does not discharge the Unit obligation.** |
| 2 | Null context | T | — | — | — | — | — | `RedirectToPage("/Error")` | yes | Unit | ✅ `OnPostAsync_NullAuthorizationContext_RedirectsToError` |
| 3 | Deny | F | no | — | — | T | I pass | denied `ConsentResponse`, `ConsentDeniedEvent`, `Redirect` | yes | Unit | ❌ Gap. Needs `_events` mock. E2E `Consent_Deny_RedirectsWithAccessDenied` covers; Unit obligation open. |
| 4 | Invalid button | F | other | — | — | F | J | `ModelState` error, re-render | yes | Unit | ❌ Gap |
| 5 | Yes, no scopes | F | yes | F | — | F | J | `MustChooseOne` error, re-render | yes | Unit | ❌ Gap. **`OnPostAsync_ButtonYes_NoScopesConsented_ReturnsPage` is named for this path but stubs `request=null`, so it actually executes row 2. Misnamed/ineffective test (defect D-2).** E2E `Consent_NoScopesConsented_ShowsError` covers the real path. |
| 6 | Offline disabled | F | yes | T | offline-off | T | I pass | scopes filtered before grant | ⏳ no | Unit | 🔧 `ConsentOptions.EnableOfflineAccess` is `static bool { get; } = true`, get-only, no seam, so `!EnableOfflineAccess` is **always false** and this branch is dead. **Defect D-1:** inject the option (make the branch live + testable) or delete the dead branch. |
| 7 | ReturnUrl null on grant | F | no | — | — | T | I throws | `ThrowIfNull` throws | yes | Unit | ❌ Gap (negative test) |
| 8 | Re-render to Page | F | other | — | — | F | J false | `Page()` (SetViewModel true) | yes | Unit | ❌ Gap. Needs constructable request. |
| 9 | Re-render to Error | F | other | — | — | F | J true | `RedirectToPage("/Error")` (SetViewModel false) | yes | Unit | ❌ Gap |

`tests_to_author` (reachable): happy + D + E(no) + E(other) + F + I-throw + J(true/false split) ≈ **8**.
Currently authored at Unit: **1 genuine** (row 2) plus **1 misnamed** (claims row 5, executes row 2).

---

#### `CreateConsentViewModel(AuthorizationRequest request)` (private)
Decisions: **D** = `ClientName ?? ClientId`; **F** = `GetValues(...) ?? Empty`; **G** = `foreach`
over `ParsedScopes` (0 vs >=1); **H** = `apiScope != null`; **J** = `EnableOfflineAccess && Resources.OfflineAccess`;
plus compound predicates `Input == null || ScopesConsented.Contains(...)` (E, I, K).

| # | Path | Decision flipped | Reachable? | Level | Status |
|---|---|---|---|---|---|
| 1 | Happy: client name, scopes present, api scope found, offline on+requested | baseline | yes | Unit | ❌ Gap (constructable request) |
| 2 | `ClientName` null -> falls back to `ClientId` | D | yes | Unit | ❌ Gap |
| 3 | `GetValues` returns null -> empty resources | F | yes | Unit | ❌ Gap |
| 4 | Zero parsed scopes | G (0 iter) | yes | Unit | ❌ Gap |
| 5 | `FindApiScope` returns null -> scope skipped | H false | yes | Unit | ❌ Gap |
| 6 | `Input == null` short-circuit (checkbox default-on) | E/I/K left | yes | Unit | ❌ Gap |
| 7 | Offline requested but `EnableOfflineAccess` false | J left operand | ⏳ no | Unit | 🔧 Same dead static as OnPost row 6 (defect D-1). Left operand hardcoded true. |

---

## Class: `PasskeysModel` — 36 uncovered branches → ~15 tests
`Identity/Identity/Pages/Account/Manage/Passkeys.cshtml.cs`
No seam needed: `UserManager<IdentityUser<Guid>>` / `SignInManager<IdentityUser<Guid>>` are mocked by
the existing Identity.Tests infrastructure; the private `DeletePasskey` is reached via the
`OnPostUpdatePasskeyAsync` switch.

#### `OnGetAsync()` — `tests_to_author = 2`

| # | Path | `user is null` | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|
| 1 | No user | T | `NotFound(...)` | yes | Unit | ❌ |
| 2 | Happy | F | `CurrentPasskeys` loaded, `Page()` | yes | Unit | ❌ |

#### `OnPostUpdatePasskeyAsync()` — `tests_to_author = 6`
Decisions: **A** `user is null`; **B** `IsNullOrWhiteSpace(Input?.CredentialId)`; **C**
`DecodeFromChars` throws `FormatException`; **D** three-way `Input.Action` (`rename` / `delete` / other).

| # | Path | A | B | C | D | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|---|---|---|
| 1 | No user | T | — | — | — | `NotFound` | yes | Unit | ❌ |
| 2 | Blank credential id | F | T | — | — | "Could not find the passkey" + redirect | yes | Unit | ❌ |
| 3 | Malformed base64 | F | F | throws | — | "invalid format" + redirect | yes | Unit | ❌ |
| 4 | Action `rename` | F | F | ok | rename | redirect `./RenamePasskey` | yes | Unit | ❌ |
| 5 | Action `delete` | F | F | ok | delete | `DeletePasskey(...)` | yes | Unit | ❌ |
| 6 | Action unknown | F | F | ok | other | "Unknown action" + redirect | yes | Unit | ❌ |

#### `DeletePasskey(user, credentialId)` (private; via row 5) — `tests_to_author = 2`

| # | Path | `!result.Succeeded` | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|
| 1 | Success | F | "The passkey was removed." + redirect | yes | Unit | ❌ |
| 2 | Failure | T | `throw InvalidOperationException` | yes | Unit | ❌ (negative) |

#### `OnPostAddPasskeyAsync()` — `tests_to_author = 6`
Decisions: **A** `user is null`; **B** `!IsNullOrWhiteSpace(Input?.Passkey?.Error)`; **C**
`IsNullOrWhiteSpace(Input?.Passkey?.CredentialJson)`; **D** `!attestationResult.Succeeded`; **E**
`!setPasskeyResult.Succeeded`.

| # | Path | flipped | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|
| 1 | No user | A | `NotFound` | yes | Unit | ❌ |
| 2 | Browser-reported error | B | "Could not add a passkey: {error}" + redirect | yes | Unit | ❌ |
| 3 | No credential json | C | "browser did not provide a passkey" + redirect | yes | Unit | ❌ |
| 4 | Attestation fails | D | "Could not add the passkey: {failure}" + redirect | yes | Unit | ❌ |
| 5 | Persist fails | E | "could not be added to your account" + redirect | yes | Unit | ❌ |
| 6 | Happy | none | redirect `./RenamePasskey?id={encoded}` | yes | Unit | ❌ |

`PasskeysModel` total: **15** (2 + 6 + 1-new-from-Delete + 6); all Unit, no escalation, no seam.

---

## Class: `DeviceModel` — 29 uncovered branches → ~13 tests
`Identity/Identity/Pages/Account/Manage/Device.cshtml.cs`
Device-flow twin of `ConsentModel`, extends `ConsentPageModelBase`. Deps `IDeviceFlowInteractionService`
/ `IEventService` are mocked interfaces, no seam needed. **Shares the D-1 dead branch** behind the
get-only `ConsentOptions.EnableOfflineAccess` static (lines 82, 174) — those rows are retained
(lossless) and tagged ⏳ unreachable.

#### `OnGetAsync(string? userCode)` — `tests_to_author = 3`
Decisions: **A** `IsNullOrWhiteSpace(userCode)`; **B** `!SetViewModelAsync(userCode)`.

| # | Path | A | B | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|---|
| 1 | Blank code | T | — | `Page()` (entry form) | yes | Unit | ❌ |
| 2 | Invalid code | F | T | model error + `Page()` | yes | Unit | ❌ |
| 3 | Valid code | F | F | `Input` set, `Page()` | yes | Unit | ❌ |

#### `OnPostAsync()` — `tests_to_author = 7` (1 row dead)
Decisions: **C** `ThrowIfNull(userCode)`; **D** `request == null`; **E** three-way `Button`
(`no`/`yes`/other); **F** `ScopesConsented.Any()` (under yes); **G** `!EnableOfflineAccess` (under
yes+scopes); **H** `grantedConsent != null`; **I** `!SetViewModelAsync` (under H false).

| # | Path | C | D | E | F | G | H | Outcome | Reach? | Level | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Null user code | throws | — | — | — | — | — | `ArgumentNullException` | yes | Unit | ❌ (negative) |
| 2 | Null context | ok | T | — | — | — | — | redirect `/Error` | yes | Unit | ❌ |
| 3 | Deny | ok | F | no | — | — | T | `ConsentDeniedEvent` + metrics → `HandleRequest` + redirect `DeviceSuccess` | yes | Unit | ❌ |
| 4 | Grant | ok | F | yes | T | offline-on | T | `ConsentGrantedEvent` + metrics → redirect `DeviceSuccess` | yes | Unit | ❌ |
| 5 | Yes, no scopes | ok | F | yes | F | — | F | `MustChooseOne` error → re-render/Error | yes | Unit | ❌ |
| 6 | Invalid button | ok | F | other | — | — | F | `InvalidSelection` error → re-render/Error | yes | Unit | ❌ |
| 7 | Re-render fails | ok | F | other | — | — | F | `!SetViewModel` → redirect `/Error` | yes | Unit | ❌ |
| 8 | Offline disabled | ok | F | yes | T | offline-off | T | scopes filtered before grant | ⏳ no | Unit | 🔧 D-1: `!EnableOfflineAccess` dead (static const true). Retained. |

#### `SetViewModelAsync(userCode)` + `CreateConsentViewModel(request)` (private; via OnGet/OnPost) — `tests_to_author = 3` (+2 dead)
Decisions: `request != null`; `ClientName ?? ClientId`; `foreach ParsedScopes` (0 vs ≥1);
`apiScope != null`; `EnableOfflineAccess && OfflineAccess`; and `Input == null || ScopesConsented.Contains(...)`.

| # | Path | Decision flipped | Reach? | Level | Status |
|---|---|---|---|---|---|
| 1 | Happy: name, scopes, api scope found, offline on+requested | baseline | yes | Unit | ❌ |
| 2 | `ClientName` null → `ClientId` fallback | `?? ClientId` | yes | Unit | ❌ |
| 3 | `FindApiScope` null → scope skipped | `apiScope != null` F | yes | Unit | ❌ |
| 4 | Zero parsed scopes | `foreach` 0 iter | yes | Unit | ❌ |
| 5 | `Input == null` short-circuit | OR left operand | ⏳ no | Unit | `Input` is a `[BindProperty]` initialized to `new InputModel()`; null is practically unreachable. Retained; mirrors the same pattern flagged in ConsentModel. |
| 6 | Offline requested, `EnableOfflineAccess` false | `&&` left operand | ⏳ no | Unit | 🔧 D-1 dead static (line 174). Retained. |

`DeviceModel` total reachable: **13**; 3 rows retained dead behind D-1 / the initialized `Input`.

---

## Class: `EmailModel` — `tests_to_author = 13`
`Identity/Identity/Pages/Account/Manage/Email.cshtml.cs`. No seam; `UserManager` + `ServiceBusSender`
mocked.

#### `OnGetAsync()` — 2
| # | `user is null` | Outcome | Status |
|---|---|---|---|
| 1 | T | `NotFound` | ❌ |
| 2 | F | `Email`/`Input`/`IsEmailConfirmed` set, `Page()` | ❌ |

#### `OnPostChangeEmailAsync()` — 6
Decisions: **A** `user is null`; **B** `!ModelState.IsValid`; **C** `!IsNullOrWhiteSpace(NewEmail) && NewEmail != email`; **D** `!IsNullOrWhiteSpace(callbackUrl)`.
| # | Path | A | B | C | D | Outcome | Status |
|---|---|---|---|---|---|---|---|
| 1 | No user | T | — | — | — | `NotFound` | ❌ |
| 2 | Invalid model | F | T | — | — | repopulate + `Page()` | ❌ |
| 3 | Changed, url ok | F | F | T | T | token + `SendMessageAsync` + "sent" redirect | ❌ |
| 4 | Changed, url null | F | F | T | F | skip send, "sent" redirect | ❌ |
| 5 | Blank new email | F | F | C-left F | — | "unchanged" redirect | ❌ |
| 6 | Same as current | F | F | C-right F | — | "unchanged" redirect | ❌ |

#### `OnPostSendVerificationEmailAsync()` — 5
Decisions: **A** `user is null`; **B** `!ModelState.IsValid`; **C** `!IsNullOrWhiteSpace(email) && !IsNullOrWhiteSpace(callbackUrl)`.
| # | Path | A | B | C | Outcome | Status |
|---|---|---|---|---|---|---|
| 1 | No user | T | — | — | `NotFound` | ❌ |
| 2 | Invalid model | F | T | — | repopulate + `Page()` | ❌ |
| 3 | Email + url ok | F | F | both T | `SendMessageAsync` + "sent" redirect | ❌ |
| 4 | Email blank | F | F | C-left F | skip send, "sent" redirect | ❌ |
| 5 | url null | F | F | C-right F | skip send, "sent" redirect | ❌ |

---

## Class: `RenamePasskeyModel` — `tests_to_author = 10`
`Identity/Identity/Pages/Account/Manage/RenamePasskey.cshtml.cs`. No seam; `UserManager` mocked.
**Fixture note:** `OnPostAsync` uses EF Core `ApplicationDbContext.UserPasskeys.SingleOrDefaultAsync`, so
its tests need the in-memory/SQLite `ApplicationDbContext` fixture the Identity suite already provides.

#### `OnGetAsync(string id)` — 4
| # | Path | Outcome | Status |
|---|---|---|---|
| 1 | `user is null` | `NotFound` | ❌ |
| 2 | `DecodeFromChars` throws `FormatException` | "invalid format" → redirect `./Passkeys` | ❌ |
| 3 | `passkey is null` | `NotFound` | ❌ |
| 4 | Happy | `Input` (CredentialId+Name) set, `Page()` | ❌ |

#### `OnPostAsync()` — 6
Decisions: `user is null`; `FormatException`; `passkey is null`; `!result.Succeeded`; `passkeyEntity is not null`.
| # | Path | Outcome | Status |
|---|---|---|---|
| 1 | `user is null` | `NotFound` | ❌ |
| 2 | malformed credential id | "invalid format" → redirect `./Passkeys` | ❌ |
| 3 | `passkey is null` | `NotFound` | ❌ |
| 4 | `AddOrUpdatePasskey` fails | `throw InvalidOperationException` | ❌ (negative) |
| 5 | Happy, `passkeyEntity` present | EF name update + `SaveChanges`, "updated" redirect | ❌ |
| 6 | Happy, `passkeyEntity` null | skip EF update, "updated" redirect | ❌ |

---

## Class: `DeletePersonalDataModel` — `tests_to_author = 8`
`Identity/Identity/Pages/Account/Manage/DeletePersonalData.cshtml.cs`. No seam; `UserManager` +
`SignInManager` mocked.

#### `OnGet()` — 2
| # | `user is null` | Outcome | Status |
|---|---|---|---|
| 1 | T | `NotFound` | ❌ |
| 2 | F | `RequirePassword` set, `Page()` | ❌ |

#### `OnPostAsync()` — 6
Decisions: **A** `user is null`; **B** `RequirePassword && (IsNullOrWhiteSpace(Password) || !CheckPassword)`; **C** `!result.Succeeded`.
| # | Path | A | B (RequirePwd / blank / wrong) | C | Outcome | Status |
|---|---|---|---|---|---|---|
| 1 | No user | T | — | — | `NotFound` | ❌ |
| 2 | No password required | F | RequirePwd F | F | `DeleteAsync` + `SignOut` + `Redirect("~/")` | ❌ |
| 3 | Required, blank | F | T + blank | — | "Incorrect password" + `Page()` | ❌ |
| 4 | Required, wrong | F | T + wrong | — | "Incorrect password" + `Page()` | ❌ |
| 5 | Required, correct | F | T + correct | F | delete + `SignOut` + `Redirect` | ❌ |
| 6 | Delete fails | F | (any pass) | T | `throw InvalidOperationException` | ❌ (negative) |

---

## Class: `ExternalLoginModel` — `tests_to_author = 19` (the largest handler)
`Identity/Identity/Pages/Account/ExternalLogin.cshtml.cs`. No seam; `SignInManager` / `UserManager` /
`IUserStore` / `ServiceBusSender` mocked. Private `CompleteExternalRegistrationAsync` reached via
`OnPostConfirmationAsync`.

#### `OnGet()` / `OnPost(provider, returnUrl)` — 2 (straight-line)
| # | Method | Outcome | Status |
|---|---|---|---|
| 1 | `OnGet` | `RedirectToPage("./Login")` | ❌ |
| 2 | `OnPost` | `ChallengeResult(provider, properties)` | ❌ |

#### `OnGetCallbackAsync(returnUrl, remoteError)` — 7
Decisions: `!IsNullOrWhiteSpace(remoteError)`; `info is null`; `result.Succeeded` →
`Url.IsLocalUrl(returnUrl)` (open-redirect guard); `result.IsLockedOut`; `Principal.HasClaim(Email)`.
| # | Path | Outcome | Status |
|---|---|---|---|
| 1 | `remoteError` set | error → redirect `./Login` | ❌ |
| 2 | `info is null` | error → redirect `./Login` | ❌ |
| 3 | Success, local `returnUrl` | `LocalRedirect(returnUrl)` | ❌ |
| 4 | Success, non-local `returnUrl` | `LocalRedirect("~/")` (open-redirect blocked) | ❌ |
| 5 | Locked out | redirect `./Lockout` | ❌ |
| 6 | Else, principal has email claim | `Input.Email` set, `Page()` | ❌ |
| 7 | Else, no email claim | `Page()` (no `Input`) | ❌ |

#### `OnPostConfirmationAsync(returnUrl)` + `CompleteExternalRegistrationAsync(...)` — 8
Decisions: `info is null`; `ModelState.IsValid && !IsNullOrWhiteSpace(Email)`; `CreateAsync` `result.Succeeded`;
`redirect is not null`; `foreach result.Errors`; then in the private: `!AddLogin.Succeeded`;
`!IsNullOrWhiteSpace(callbackUrl)`; `RequireConfirmedAccount`; `Url.IsLocalUrl(returnUrl)`.
| # | Path | Outcome | Status |
|---|---|---|---|
| 1 | `info is null` | error → redirect `./Login` | ❌ |
| 2 | Model invalid / email blank | `Page()` (AND false) | ❌ |
| 3 | Create fails | errors added, `Page()` | ❌ |
| 4 | Create ok, `AddLogin` fails → `redirect` null | errors loop, `Page()` | ❌ |
| 5 | Registered, `callbackUrl` null | skip confirmation email | ❌ |
| 6 | Registered, `RequireConfirmedAccount` true | redirect `./RegisterConfirmation` | ❌ |
| 7 | Registered, no confirm, local `returnUrl` | `SignIn` + `LocalRedirect(returnUrl)` | ❌ |
| 8 | Registered, no confirm, non-local `returnUrl` | `SignIn` + `LocalRedirect("~/")` (open-redirect blocked) | ❌ |

---

## Identity page-models roll-up

| Class | tests_to_author | Seam |
|---|---|---|
| `ConsentModel` (pilot) | ~8 reachable (+ dead/parked rows) | none |
| `PasskeysModel` | 15 | none |
| `DeviceModel` | 13 (+3 dead) | none |
| `EmailModel` | 13 | none |
| `RenamePasskeyModel` | 10 | none (EF `ApplicationDbContext` fixture) |
| `DeletePersonalDataModel` | 8 | none |
| `ExternalLoginModel` | 19 | none |
| **Total** | **~86** | no production seams — all reachable via existing Identity mock infra |

Identity needs **no accessibility seams**: every page-model handler is `public`, and its `UserManager`
/ `SignInManager` / Duende interaction services / `ApplicationDbContext` dependencies are already
mockable through the test infrastructure that backs the 510 existing tests. The two open-redirect
guards (`ExternalLoginModel` callback + confirmation) each get an explicit local/non-local row pair,
matching the project's "never `LocalRedirect(returnUrl)` directly" rule.

---

## Defects backlog (surfaced by the pilot)

| ID | Type | Location | Action |
|---|---|---|---|
| D-1 | 🔧 Missing seam / dead branch | `ConsentOptions.EnableOfflineAccess` (`static bool { get; } = true`) gates `Consent.cshtml.cs:76` and `:192` (and identically `Device.cshtml.cs:82,174`). | The `!EnableOfflineAccess` branch is unreachable. Inject the option (`IOptions<ConsentOptions>` or a settable seam) to make it live and testable, or delete the dead branch. Until then those rows sit ⏳. |
| D-2 | Misnamed / ineffective test | `Consent.cshtmlTests.cs` `OnPostAsync_ButtonYes_NoScopesConsented_ReturnsPage` | The test stubs `GetAuthorizationContextAsync => null`, so it executes the `request == null -> /Error` path (row 2), not the "yes + no scopes" path it is named for. Rebuild it with a constructed `AuthorizationRequest` so it actually drives row 5. |
| D-3 | Falsified "untestable" claim | Same test file, comment lines ~107-116 | The comment asserts `AuthorizationRequest` "cannot be easily constructed." It has a `// public for testing` parameterless ctor; `Client` and `ValidatedResources` are settable; `ResourceValidationResult` is constructable. Delete the comment and write the real unit tests for rows 1, 3, 4, 5, 7, 8, 9. |

**Net:** the gnarliest method in the repo has ~8 required unit tests, ~1 genuinely present, 1 dead
branch behind a hardcoded static, and 1 test misnamed for a path it never runs. That is the
robustness result: the methodology, applied honestly, converts a vague "it's covered by E2E" into a
concrete, prioritized backlog.
