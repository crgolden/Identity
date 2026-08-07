namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ShowRecoveryCodesModelTests
{
    private static readonly string[] SingleCode = ["ABC123"];
    private static readonly string[] DuplicateCodes = ["code", "code"];
    private static readonly string[] EmptyWhitespaceCodes = [string.Empty, "   "];

    public static TheoryData<string[]> InvalidRecoveryCodes() => new()
    {
        Array.Empty<string>(),
    };

    public static TheoryData<string[]> ValidRecoveryCodes() => new()
    {
        SingleCode,
        DuplicateCodes,
        EmptyWhitespaceCodes,
        CreateLargeArray(10000, "X"),
    };

    [Theory]
    [MemberData(nameof(InvalidRecoveryCodes))]
    public void OnGet_RecoveryCodesEmpty_RedirectsToTwoFactorAuthentication(string[] recoveryCodes)
    {
        // Arrange
        var model = new ShowRecoveryCodesModel
        {
            RecoveryCodes = recoveryCodes,
        };

        // Act
        var result = model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./TwoFactorAuthentication", redirect.PageName);
    }

    [Theory]
    [MemberData(nameof(ValidRecoveryCodes))]
    public void OnGet_RecoveryCodesHasItems_ReturnsPageResult(string[] recoveryCodes)
    {
        // Arrange
        var model = new ShowRecoveryCodesModel
        {
            RecoveryCodes = recoveryCodes,
        };

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    private static string[] CreateLargeArray(int count, string value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var arr = new string[count];
        for (var i = 0; i < count; i++)
        {
            arr[i] = value;
        }

        return arr;
    }
}