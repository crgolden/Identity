namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ShowRecoveryCodesModelTests
{
    private const int LargeRecoveryCodeCount = 10_000;

    private static readonly string[] SingleCode = [TestValues.NewRecoveryCode()];
    private static readonly string[] DuplicateCodes = DuplicatesOf(TestValues.NewRecoveryCode());
    private static readonly string[] EmptyWhitespaceCodes = [string.Empty, TestValues.NewWhitespaceValue()];

    public static TheoryData<string[]> InvalidRecoveryCodes() => new()
    {
        Array.Empty<string>(),
    };

    public static TheoryData<string[]> ValidRecoveryCodes() => new()
    {
        SingleCode,
        DuplicateCodes,
        EmptyWhitespaceCodes,
        CreateLargeArray(LargeRecoveryCodeCount, TestValues.NewRecoveryCode()),
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
        Assert.Equal(PageRoutes.SiblingTwoFactorAuthentication, redirect.PageName);
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

    private static string[] DuplicatesOf(string value) => [value, value];

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