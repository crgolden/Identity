namespace Identity.Tests.Unit.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Infrastructure;
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
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<IdentityUser<Guid>>>.Instance;

        var userManager = new UserManager<IdentityUser<Guid>>(
            storeMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errors,
            services.Object,
            logger);

        // Act
        var model = new ResetPasswordModel(userManager);

        // Assert
        Assert.NotNull(model);
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

        // Act
        var exception = Record.Exception(() => model.OnGet(malformed));

        // Assert
        Assert.IsType<FormatException>(exception);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var newPassword = TestValues.NewPassword();
        var model = new ResetPasswordModel(userManagerMock.Object);
        model.Input = new ResetPasswordModel.InputModel
        {
            Email = TestValues.NewEmailAddress(),
            Password = newPassword,
            ConfirmPassword = newPassword,
            Code = TestValues.NewProviderKey()
        };

        model.ModelState.AddModelError(nameof(ResetPasswordModel.InputModel.Email), TestValues.NewFailureReason());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UnknownEmail_RedirectsToConfirmationWithoutResettingAPassword()
    {
        // Arrange
        var unknownEmail = $"{Guid.NewGuid():N}@example.com";
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.FindByEmailAsync(unknownEmail))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = BuildResetPasswordModel(userManagerMock, unknownEmail, out _, out _);

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("./ResetPasswordConfirmation", redirect.PageName);
        userManagerMock.Verify(um => um.FindByEmailAsync(unknownEmail), Times.Once);
        userManagerMock.Verify(
            um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ResetSucceeds_RedirectsToConfirmation()
    {
        // Arrange
        var resettingEmail = $"{Guid.NewGuid():N}@example.com";
        var resettingUser = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            Email = resettingEmail,
            UserName = resettingEmail
        };

        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.FindByEmailAsync(resettingEmail))
            .ReturnsAsync(resettingUser);
        userManagerMock
            .Setup(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var model = BuildResetPasswordModel(userManagerMock, resettingEmail, out var resetCode, out var newPassword);

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("./ResetPasswordConfirmation", redirect.PageName);
        userManagerMock.Verify(um => um.FindByEmailAsync(resettingEmail), Times.Once);
        userManagerMock.Verify(um => um.ResetPasswordAsync(resettingUser, resetCode, newPassword), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var resettingEmail = TestValues.NewEmailAddress();
        var resetCode = TestValues.NewProviderKey();
        var newPassword = TestValues.NewPassword();
        var firstError = TestValues.NewFailureReason();
        var secondError = TestValues.NewFailureReason();
        var foundUser = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            Email = resettingEmail,
            UserName = TestValues.NewUserName()
        };

        userManagerMock
            .Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == resettingEmail)))
            .ReturnsAsync(foundUser);

        var errors = new[]
        {
            new IdentityError { Description = firstError },
            new IdentityError { Description = secondError }
        };
        var failedResult = IdentityResult.Failed(errors);

        userManagerMock
            .Setup(um => um.ResetPasswordAsync(foundUser, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(failedResult);

        var model = new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = resettingEmail,
                Password = newPassword,
                ConfirmPassword = newPassword,
                Code = resetCode
            }
        };

        Assert.True(model.ModelState.IsValid);

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(actionResult);
        Assert.False(model.ModelState.IsValid);
        var entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        var actualMessages = entry.Errors.Select(e => e.ErrorMessage).ToArray();
        Assert.Contains(firstError, actualMessages);
        Assert.Contains(secondError, actualMessages);
        userManagerMock.Verify(um => um.FindByEmailAsync(resettingEmail), Times.Once);
        userManagerMock.Verify(um => um.ResetPasswordAsync(foundUser, resetCode, newPassword), Times.Once);
    }

    private static ResetPasswordModel BuildResetPasswordModel(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        string email,
        out string resetCode,
        out string newPassword)
    {
        resetCode = Guid.NewGuid().ToString("N");
        newPassword = $"NewP@ss{Guid.NewGuid():N}";

        return new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = email,
                Password = newPassword,
                ConfirmPassword = newPassword,
                Code = resetCode
            }
        };
    }
}