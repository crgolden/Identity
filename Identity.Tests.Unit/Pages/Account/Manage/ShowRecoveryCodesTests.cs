namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ShowRecoveryCodesTests
{
    private const int LargeRecoveryCodeCount = 10_000;

    public static TheoryData<string[]> InvalidRecoveryCodes() => new()
    {
        Array.Empty<string>(),
    };

    [Theory]
    [MemberData(nameof(InvalidRecoveryCodes))]
    public void OnGet_RecoveryCodesEmpty_RedirectsToTwoFactorAuthentication(string[] recoveryCodes)
    {
        // Arrange
        var model = new ShowRecoveryCodes
        {
            RecoveryCodes = recoveryCodes,
        };

        // Act
        var result = model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingTwoFactorAuthentication, redirect.PageName);
    }

    [Fact]
    public void OnGet_SingleRecoveryCode_ReturnsPageResult()
    {
        // Arrange
        var recoveryCode = Generated.NewRecoveryCode();
        var model = new ShowRecoveryCodes
        {
            RecoveryCodes = [recoveryCode],
        };

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public void OnGet_DuplicateRecoveryCodes_ReturnsPageResult()
    {
        // Arrange
        var duplicatedRecoveryCode = Generated.NewRecoveryCode();
        var model = new ShowRecoveryCodes
        {
            RecoveryCodes = DuplicatesOf(duplicatedRecoveryCode),
        };

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public void OnGet_BlankRecoveryCodes_ReturnsPageResult()
    {
        // Arrange
        var whitespaceRecoveryCode = Generated.NewWhitespaceValue();
        var model = new ShowRecoveryCodes
        {
            RecoveryCodes = [Generated.NewBlank(), whitespaceRecoveryCode],
        };

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public void OnGet_LargeNumberOfRecoveryCodes_ReturnsPageResult()
    {
        // Arrange
        var repeatedRecoveryCode = Generated.NewRecoveryCode();
        var model = new ShowRecoveryCodes
        {
            RecoveryCodes = CreateLargeArray(LargeRecoveryCodeCount, repeatedRecoveryCode),
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
