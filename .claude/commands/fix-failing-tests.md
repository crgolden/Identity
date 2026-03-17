# Fix Failing Tests

Fix all failing tests in this repository. For unit tests, prefer fixing tests over source. For E2E tests, bugs in the source are more likely — check both carefully before deciding which to fix.

## Workflow

1. Run `ASPNETCORE_ENVIRONMENT=Development dotnet test --configuration Release` and collect all failures.
2. For each failing test, read both the **test file** and the **source file** it exercises.
3. Determine whether the test's expectation disagrees with the source's actual behavior.
4. For unit tests: fix the test unless the source has a genuine bug. For E2E tests: trace the full HTTP request sequence through server logs to find the actual failure point before concluding where the bug is.
5. Re-run tests to confirm zero failures before finishing.

## Rules

- For unit tests: prefer fixing tests over source. Tests document intended behavior; if a test is stale, fix it.
- For E2E tests: test selectors and source logic are both suspect. Read the Razor pages to verify selectors before assuming the source is correct.
- Fix tests in parallel where possible (independent files can be edited simultaneously).
- After every batch of edits, re-run tests to catch regressions.
- When diagnosing E2E failures: filter server log output for `HTTP GET|HTTP POST|responded|redirect` to trace the exact request sequence rather than guessing.

## Common Failure Patterns (learned from this codebase)

### 1. Log-level mismatch
Source uses `LogTrace` / `LogDebug` but test verifies `LogInformation` (or vice versa).
**Fix:** Update the `LogLevel.X` argument in the `loggerMock.Verify(...)` call to match the source.

### 2. Property initialized to empty collection, not null
Source declares `public IList<T> Foo { get; set; } = new List<T>();` or `= Empty<T>()`.
Test asserts `Assert.Null(model.Foo)`.
**Fix:** Replace with `Assert.NotNull(model.Foo); Assert.Empty(model.Foo);`

### 3. Null-or-whitespace guard redirects before reaching the tested branch
Source has an early-return guard like `if (IsNullOrWhiteSpace(Input?.Password)) return Page();`.
Test does not set `Input`, so it hits the guard and returns `Page()` instead of reaching
the `NotFound` / redirect the test expects.
**Fix:** Assign a valid `model.Input = new InputModel { Password = "ValidP@ss1!" };` before calling the handler.

### 4. IsNullOrWhiteSpace vs IsNullOrEmpty — whitespace/control-char inputs
Source uses `!IsNullOrWhiteSpace(value)` as a boolean property (e.g. `ShowRequestId`).
Test supplies `" "`, `"\n"`, `"\r\n"` and expects `true`.
`IsNullOrWhiteSpace` returns `true` for those strings, so the property returns `false`.
**Fix:** Update the test's `expected` value to `false` for whitespace-only inputs, or remove those cases if the intent was to test `IsNullOrEmpty` behavior.

### 5. Empty-string as a valid encoded value
`WebEncoders.Base64UrlEncode(new byte[0])` produces `""`.
If the source guards with `IsNullOrWhiteSpace(code)`, passing `""` returns `BadRequest`, not `PageResult`.
**Fix:** Remove `string.Empty` from the theory's valid-encoded-input cases.

### 6. MemberData includes null/empty values that are legitimately filtered by the source
Source conditionally skips logic for null/empty inputs (e.g., `!IsNullOrWhiteSpace(email)` before sending email).
Test's `MemberData` includes `null` and `""` and expects the full happy-path to execute.
**Fix:** Remove the null / empty entries from `MemberData` so they don't exercise the skipped branch.

### 7. Guard produces redirect, not the expected result type
Source: `if (IsNullOrWhiteSpace(email)) return RedirectToPage("/Index");`
Test supplies `""` or `"   "` and asserts `Assert.IsType<NotFoundObjectResult>(result)`.
**Fix:** Either remove those inputs from the theory, or split into a separate test that asserts `RedirectToPageResult` with the correct page name.

### 8. RecoveryCodes / similar TempData arrays — null assignment on non-nullable property
Source: `public string[] RecoveryCodes { get; set; } = Empty<string>();`
Test sets `RecoveryCodes = null` and expects `RedirectToPageResult`, but source calls `.Length` on it → `NullReferenceException`.
**Fix:** Remove `null` from the invalid-input `MemberData`; only test the empty-array case.

## E2E Failure Patterns (Playwright + ASP.NET Core Identity)

### 9. `button[type='submit']` clicks the nav logout button on authenticated pages
The `_LoginPartial.cshtml` nav bar contains `<button type="submit" class="nav-link btn btn-link text-dark">Logout</button>` inside a logout form. It appears before the main form in DOM order, so `page.ClickAsync("button[type='submit']")` clicks it instead of the intended submit button on any page where the user is logged in.
**Fix:** Use a CSS class specific to the form button — `button.btn-primary` (most forms) or `button.btn-danger` (delete/destructive actions). Never use `button[type='submit']` on authenticated pages.

### 10. `<kbd>` element confused for `<code>`
The `EnableAuthenticator.cshtml` page renders the TOTP shared key inside `<kbd>`, not `<code>` or a custom `#sharedKey` element. Selectors like `#sharedKey, [data-shared-key], code` all miss it.
**Fix:** Use `kbd` as the Playwright locator selector.

### 11. Playwright cookies not sent — Kestrel must use HTTPS
ASP.NET Core Identity auth cookies are marked `Secure`. If the test Kestrel server listens on plain HTTP, the browser never sends them on subsequent requests, making every post-login page redirect back to `/Account/Login`.
**Fix:** In `IdentityWebApplicationFactory.CreateHost`, configure Kestrel with `lo.UseHttps()` and set `IgnoreHTTPSErrors = true` on the Playwright browser context. Confirm the server is on HTTPS by checking that server logs show `https://` in request URLs.

### 12. `WaitForURLAsync` timeout caused by wrong HTTP request being observed in logs
Playwright `WaitForURLAsync` / `WaitForNavigationAsync` timeouts are often misleading — the reported URL may already be matching but a prior navigation (e.g., a form POST to the wrong endpoint) redirected away first. Always trace the full sequence of `HTTP POST` and redirect responses in server logs before concluding what failed.
**Fix:** Run with `--show-live-output on` and grep for `HTTP GET|HTTP POST|responded|redirect` to see the exact request chain.

### 13. `lockoutOnFailure: false` prevents lockout tests from ever passing
`SignInManager.PasswordSignInAsync` only increments the failed-access counter when called with `lockoutOnFailure: true`. Passing `false` means the `IsLockedOut` branch is unreachable regardless of how many failures occur.
**Fix:** Change the call-site in `Login.cshtml.cs` to `lockoutOnFailure: true`.

### 14. Password reset URL missing `email` parameter
`ForgotPassword.cshtml.cs` may build the reset callback URL with only `{ code }` in the route values, omitting `email`. `ResetPassword.cshtml.cs` then renders an empty email input, which fails `IsNullOrWhiteSpace` validation on POST and never redirects to `ResetPasswordConfirmation`.
**Fix:** Add `email = Input.Email` to the route values in `ForgotPassword`, and read `string? email = null` in `ResetPassword.OnGet`, storing it in `Input.Email`.

### 15. HTML-encoded URLs in email links cause confirmation/reset to silently fail
`HtmlEncoder.Default.Encode(callbackUrl)` converts `&` → `&amp;` in the href attribute. `EmailCaptureService.ExtractLink` must call `System.Net.WebUtility.HtmlDecode` on the extracted value before returning it, or Playwright navigates to a URL with `&amp;code=...` which the server treats as a null code and silently redirects away.
**Fix:** Wrap the return value in `WebUtility.HtmlDecode(...)` inside `ExtractLink`.

### 16. External HTTP calls (Gravatar, etc.) inflate test timeouts
`GravatarService.GetAvatarUrlAsync` is called during registration and can add 2–4 seconds per test for test email addresses that have no Gravatar profile. This makes all registration-based tests sensitive to timeout values.
**Fix:** Replace `IAvatarService` in `IdentityWebApplicationFactory.ConfigureWebHost` with a no-op implementation that returns `null` immediately. Remove the stub once all tests are passing.

## Useful Moq patterns for this codebase

```csharp
// Verify LogTrace (not LogInformation)
loggerMock.Verify(
    l => l.Log(
        LogLevel.Trace,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("expected phrase")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
    Times.Once);

// Minimal UserManager mock (no full DI setup needed for most tests)
var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
    storeMock.Object, null, null, null, null, null, null, null, null);

// Minimal SignInManager mock
var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
    userManagerMock.Object,
    Mock.Of<IHttpContextAccessor>(),
    Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
    null, null, null, null);
```
