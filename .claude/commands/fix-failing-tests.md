# Fix Failing Unit Tests

Fix all failing unit tests in this repository **without modifying source/business logic**. Tests must be updated to accurately reflect what the source code actually does.

## Workflow

1. Run `dotnet test --project Identity.Tests/Identity.Tests.csproj` and collect all failures.
2. For each failing test, read both the **test file** and the **source file** it exercises.
3. Determine whether the test's expectation disagrees with the source's actual behavior.
4. Fix the test — never the source — unless the source has a genuine bug unrelated to a stale test.
5. Re-run tests to confirm zero failures before finishing.

## Rules

- **Never modify source files** to satisfy a test. Tests document behavior; if a test is wrong, fix the test.
- Fix tests in parallel where possible (independent files can be edited simultaneously).
- After every batch of edits, re-run tests to catch regressions.

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
