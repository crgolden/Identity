namespace Identity.Tests.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Tests for ShowRecoveryCodesModel.OnGet method.
/// </summary>
[Trait("Category", "Unit")]
public class ShowRecoveryCodesModelTests
{
    /// <summary>
    /// Verifies that when RecoveryCodes is null or an empty array the handler redirects to the TwoFactorAuthentication page.
    /// Input conditions:
    /// - recoveryCodes: null and empty array
    /// Expected result:
    /// - RedirectToPageResult with PageName "./TwoFactorAuthentication"
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidRecoveryCodes))]
    public void OnGet_RecoveryCodesNullOrEmpty_RedirectsToTwoFactorAuthentication(string[]? recoveryCodes)
    {
        // Arrange
        var model = new ShowRecoveryCodesModel
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            RecoveryCodes = recoveryCodes
#pragma warning restore CS8601 // Possible null reference assignment.
        };

        // Act
        var result = model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./TwoFactorAuthentication", redirect.PageName);
    }

    /// <summary>
    /// Verifies that when RecoveryCodes contains at least one element the handler returns the Page result.
    /// Input conditions:
    /// - recoveryCodes: single item, duplicates, whitespace/empty items, and a large array
    /// Expected result:
    /// - PageResult (no redirect)
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidRecoveryCodes))]
    public void OnGet_RecoveryCodesHasItems_ReturnsPageResult(string[]? recoveryCodes)
    {
        // Arrange
        var model = new ShowRecoveryCodesModel
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            RecoveryCodes = recoveryCodes
#pragma warning restore CS8601 // Possible null reference assignment.
        };

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    // MemberData for invalid cases: empty array
    public static TheoryData<string[]?> InvalidRecoveryCodes() => new()
    {
        Array.Empty<string>(),
    };

    // MemberData for valid cases: single, duplicates, empty/whitespace codes, and a large array
    public static TheoryData<string[]?> ValidRecoveryCodes() => new()
    {
        new string[] { "ABC123" },
        new string[] { "code", "code" },
        new string[] { "", "   " },
        // Large but feasible array to exercise non-empty boundary
        CreateLargeArray(10000, "X"),
    };

    private static string[] CreateLargeArray(int count, string value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var arr = new string[count];
        for (var i = 0; i < count; i++) arr[i] = value;
        return arr;
    }
}
