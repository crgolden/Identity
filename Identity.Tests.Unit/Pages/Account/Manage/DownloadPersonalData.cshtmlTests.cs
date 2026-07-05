namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DownloadPersonalDataModelTests
{
    [Fact]
    public void OnGet_DefaultState_ReturnsNotFoundResult()
    {
        // userManager is not used by OnGet, but the constructor guards against null, so pass a real mock.
        var model = new DownloadPersonalDataModel(MockHelpers.MockUserManager().Object);

        // Act
        var result = model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = "sentinel-user-id";
        var userManagerMock = MockHelpers.MockUserManager();

        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(userId);

        var model = new DownloadPersonalDataModel(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        var message = notFoundResult.Value.ToString() ?? string.Empty;
        Assert.Contains(userId, message);
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
        var userManagerLoggerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManager = new UserManager<IdentityUser<Guid>>(
            storeMock.Object,
            optionsMock.Object,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            new IdentityErrorDescriber(),
            serviceProviderMock.Object,
            userManagerLoggerMock.Object);

        // Act
        var model = new DownloadPersonalDataModel(userManager);

        // Assert
        Assert.NotNull(model);
        var result = model.OnGet();
        Assert.IsType<NotFoundResult>(result);
    }
}
