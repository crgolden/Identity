#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

[Trait("Category", "Unit")]
public class LogoutModelTests
{
    public static TheoryData<string?, string> RedirectCases() => new()
    {
        { "/dashboard", "/dashboard" },
        { "/", "/" },
        { null, "/" },
        { string.Empty, "/" },
        { "   ", "/" },
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NullSignInManager_DoesNotThrow(bool loggerIsNull)
    {
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = loggerIsNull ? null : new Mock<ILogger<LogoutModel>>().Object;

        var exception = Record.Exception(() =>
        {
            var model = new LogoutModel(signInManager, logger);
            Assert.NotNull(model);
        });

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(RedirectCases))]
    public async Task OnGetAsync_VariousReturnUrls_RedirectsCorrectly(string? returnUrl, string expectedUrl)
    {
        var model = BuildModel();

        var result = await model.OnGetAsync(returnUrl);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(expectedUrl, redirect.Url);
    }

    [Theory]
    [MemberData(nameof(RedirectCases))]
    public async Task OnPost_VariousReturnUrls_RedirectsCorrectly(string? returnUrl, string expectedUrl)
    {
        var model = BuildModel();

        var result = await model.OnPost(returnUrl);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(expectedUrl, redirect.Url);
    }

    private static LogoutModel BuildModel()
    {
        var userManager = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        var signInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null);
        return new LogoutModel(signInManager.Object, Mock.Of<ILogger<LogoutModel>>());
    }
}
