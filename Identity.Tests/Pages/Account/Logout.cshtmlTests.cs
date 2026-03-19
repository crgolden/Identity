#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Tests for Identity.Pages.Account.LogoutModel constructor behavior.
/// Focuses on constructor acceptance of provided dependencies and null handling.
/// </summary>
[Trait("Category", "Unit")]
public class LogoutModelTests
{
    public static TheoryData<string?, Type> GetCases() => new()
    {
        // non-null returnUrl => LocalRedirectResult expected
        { "/", typeof(LocalRedirectResult) },

        // null returnUrl => RedirectToPageResult expected
        { null, typeof(RedirectToPageResult) },

        // empty string is still non-null and will be treated as LocalRedirect by the implementation
        { string.Empty, typeof(LocalRedirectResult) },

        // whitespace is non-null => LocalRedirectResult (implementation does not validate content)
        { "   ", typeof(LocalRedirectResult) },
    };

    /// <summary>
    /// Verifies that the LogoutModel constructor does not throw when the SignInManager parameter is null
    /// and the logger parameter is provided or null. This checks that the constructor performs simple assignment
    /// and does not validate inputs.
    /// Input conditions: signInManager = null, logger = null or non-null (mocked).
    /// Expected result: constructor completes without throwing and returns a non-null LogoutModel instance.
    /// </summary>
    /// <param name="loggerIsNull">If true, pass null for logger; otherwise pass a mocked logger.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NullSignInManager_DoesNotThrow(bool loggerIsNull)
    {
        // Arrange
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = loggerIsNull ? null : new Mock<ILogger<LogoutModel>>().Object;

        // Act
        var exception = Record.Exception(() =>
        {
            // Act: construct the model
            var model = new LogoutModel(signInManager!, logger!);

            // Assert inside Act block: ensure the object is not null when constructor completes
            Assert.NotNull(model);
        });

        // Assert
        Assert.Null(exception);
    }
}