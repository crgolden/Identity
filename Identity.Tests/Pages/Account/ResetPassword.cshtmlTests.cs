namespace Identity.Tests.Pages.Account;
using Infrastructure;

using System.Text;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResetPasswordModelTests
{
    public static TheoryData<string> GetValidEncodedCases() => new()
    {
        "abc",
        "p@$$w0rd!",
        "??????",
    };

    [Fact]
    public void ResetPasswordModel_ValidUserManager_ConstructsSuccessfully()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>(MockBehavior.Loose);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManager = new UserManager<IdentityUser<Guid>>(
            storeMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errors,
            services.Object,
            logger.Object);

        // Act
        var model = new ResetPasswordModel(userManager);

        // Assert
        Assert.NotNull(model);

        // Intentionally minimal: the goal is to ensure constructor wiring works with mocked UserManager.
    }

    [Fact]
    public void OnGet_CodeIsNull_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPasswordModel(mockUserManager.Object);

        // Act
        var result = model.OnGet(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A code must be supplied for password reset.", badRequest.Value);
    }

    [Theory]
    [MemberData(nameof(GetValidEncodedCases))]
    public void OnGet_ValidBase64UrlEncodedCode_SetsInputCodeAndReturnsPage(string original)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPasswordModel(mockUserManager.Object);

        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(original));

        // Act
        var result = model.OnGet(encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        var input = model.Input;
        Assert.NotNull(input);
        Assert.Equal(original, input.Code);
    }

    [Theory]
    [InlineData("invalid!")]
    [InlineData("%%%")]
    public void OnGet_MalformedCode_ThrowsFormatException(string malformed)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPasswordModel(mockUserManager.Object);

        // Act & Assert
        Assert.Throws<FormatException>(() => model.OnGet(malformed));
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var model = new ResetPasswordModel(userManagerMock.Object);

        // Create Input but ModelState will be invalid
        model.Input = new ResetPasswordModel.InputModel
        {
            Email = "user@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            Code = "code"
        };

        // Make ModelState invalid
        model.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure user manager methods were not called
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false)] // user not found -> redirect
    [InlineData(true, true)] // user found and reset succeeds -> redirect
    public async Task OnPostAsync_UserMissingOrResetSucceeds_RedirectsToConfirmation(bool userExists, bool resetSucceeds)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        IdentityUser<Guid>? foundUser = null;
        if (userExists)
        {
            foundUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "test" };
        }

        userManagerMock
            .Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == "test@example.com")))
            .ReturnsAsync(foundUser);

        if (userExists)
        {
            var result = resetSucceeds
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "failed" });

            userManagerMock
                .Setup(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(result);
        }

        var model = new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = "test@example.com",
                Password = "NewP@ssw0rd",
                ConfirmPassword = "NewP@ssw0rd",
                Code = "code"
            }
        };

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("./ResetPasswordConfirmation", redirect.PageName);

        // Verify FindByEmailAsync called once
        userManagerMock.Verify(um => um.FindByEmailAsync("test@example.com"), Times.Once);

        if (userExists)
        {
            // ResetPasswordAsync should be called when user exists
            userManagerMock.Verify(um => um.ResetPasswordAsync(foundUser!, "code", "NewP@ssw0rd"), Times.Once);
        }
        else
        {
            // ResetPasswordAsync should not be called when user is not found
            userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    [Fact]
    public async Task OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var foundUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = "user2@example.com", UserName = "user2" };

        userManagerMock
            .Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == "user2@example.com")))
            .ReturnsAsync(foundUser);

        var errors = new[]
        {
            new IdentityError { Description = "Err1" },
            new IdentityError { Description = "Err2" }
        };
        var failedResult = IdentityResult.Failed(errors);

        userManagerMock
            .Setup(um => um.ResetPasswordAsync(foundUser, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(failedResult);

        var model = new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = "user2@example.com",
                Password = "AnotherP@ss1",
                ConfirmPassword = "AnotherP@ss1",
                Code = "code2"
            }
        };

        // Precondition: ModelState valid
        Assert.True(model.ModelState.IsValid);

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(actionResult);
        Assert.False(model.ModelState.IsValid);

        // Errors added with empty key
        var entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        var actualMessages = entry!.Errors.Select(e => e.ErrorMessage).ToArray();
        Assert.Contains("Err1", actualMessages);
        Assert.Contains("Err2", actualMessages);

        // Verify calls
        userManagerMock.Verify(um => um.FindByEmailAsync("user2@example.com"), Times.Once);
        userManagerMock.Verify(um => um.ResetPasswordAsync(foundUser, "code2", "AnotherP@ss1"), Times.Once);
    }
}