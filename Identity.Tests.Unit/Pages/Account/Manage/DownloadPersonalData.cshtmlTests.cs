namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DownloadPersonalDataModelTests
{
    [Fact]
    public void OnGet_DefaultState_ReturnsNotFoundResult()
    {
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
        var message = Assert.IsType<string>(notFoundResult.Value);
        Assert.Contains(userId, message, StringComparison.Ordinal);
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
        var model = new DownloadPersonalDataModel(userManager);

        // Assert
        Assert.NotNull(model);
        var result = model.OnGet();
        Assert.IsType<NotFoundResult>(result);
    }
}