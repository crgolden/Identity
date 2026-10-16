namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DownloadPersonalDataTests
{
    [Fact]
    public void OnGet_DefaultState_ReturnsNotFoundResult()
    {
        var model = new DownloadPersonalData(MockHelpers.MockUserManager().Object);

        // Act
        var result = model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();

        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(userId);

        var model = new DownloadPersonalData(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFoundResult.Value);
        Assert.Contains(userId, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_UserFound_ReturnsAJsonAttachmentWithTheLoginAndAuthenticatorKeys()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId(), Email = Generated.NewEmailAddress() };
        var login = new UserLoginInfo(Generated.NewSchemeName(), Generated.NewProviderKey(), Generated.NewDisplayName());
        var authenticatorKey = Generated.NewAuthenticatorKey();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetLoginsAsync(user)).ReturnsAsync(new List<UserLoginInfo> { login });
        userManagerMock.Setup(u => u.GetAuthenticatorKeyAsync(user)).ReturnsAsync(authenticatorKey);
        var httpContext = new DefaultHttpContext();
        var model = new DownloadPersonalData(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(MediaTypeNames.Application.Json, file.ContentType);
        Assert.Equal(DownloadPersonalData.AttachmentDisposition, httpContext.Response.Headers.ContentDisposition.ToString());
        var exported = JsonSerializer.Deserialize<Dictionary<string, string?>>(file.FileContents);
        Assert.NotNull(exported);
        Assert.Equal(login.ProviderKey, exported[string.Format(CultureInfo.InvariantCulture, DownloadPersonalData.ExternalLoginKeyNameFormat, login.LoginProvider)]);
        Assert.Equal(authenticatorKey, exported[DownloadPersonalData.AuthenticatorKeyName]);
        Assert.Equal(user.Email, exported[nameof(IdentityUser<Guid>.Email)]);
    }

    [Fact]
    public void Constructor_WithValidDependencies_InstanceCreatedAndOnGetReturnsNotFound()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(MockBehavior.Strict);
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var serviceProviderMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLogger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;

        var userManager = new UserManager<IdentityUser<Guid>>(
            storeMock.Object,
            optionsMock.Object,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            new IdentityErrorDescriber(),
            serviceProviderMock.Object,
            userManagerLogger);

        // Act
        var model = new DownloadPersonalData(userManager);

        // Assert
        Assert.NotNull(model);
        var result = model.OnGet();
        Assert.IsType<NotFoundResult>(result);
    }
}
