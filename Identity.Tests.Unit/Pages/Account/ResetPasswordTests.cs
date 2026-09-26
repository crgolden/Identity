namespace Identity.Tests.Unit.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResetPasswordTests
{
    private const char InvalidBase64UrlCharacter = '!';

    public static TheoryData<string> GetValidEncodedCases() => new()
    {
        Generated.NewEmailConfirmationToken(),
        Generated.NewPassword(),
        Generated.NewControlAndSymbolValue(),
    };

    public static TheoryData<string> MalformedCodes() => new()
    {
        Generated.NewUserName() + InvalidBase64UrlCharacter,
        InvalidBase64UrlCharacter + Generated.NewPathSegment(),
    };

    [Fact]
    public void ResetPasswordModel_ValidUserManager_ConstructsSuccessfully()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
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
        var model = new ResetPassword(userManager);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void OnGet_CodeIsNull_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPassword(mockUserManager.Object);

        // Act
        var result = model.OnGet();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ResetPassword.CodeRequiredMessage, badRequest.Value);
    }

    [Theory]
    [MemberData(nameof(GetValidEncodedCases))]
    public void OnGet_ValidBase64UrlEncodedCode_SetsInputCodeAndReturnsPage(string original)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPassword(mockUserManager.Object);

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
    [MemberData(nameof(MalformedCodes))]
    public void OnGet_MalformedCode_ThrowsFormatException(string malformed)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new ResetPassword(mockUserManager.Object);

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

        var newPassword = Generated.NewPassword();
        var model = new ResetPassword(userManagerMock.Object);
        model.Input = new ResetPassword.InputModel
        {
            Email = Generated.NewEmailAddress(),
            Password = newPassword,
            ConfirmPassword = newPassword,
            Code = Generated.NewProviderKey()
        };

        model.ModelState.AddModelError(nameof(ResetPassword.InputModel.Email), Generated.NewFailureReason());

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
        var unknownEmail = Generated.NewEmailAddress();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.FindByEmailAsync(unknownEmail))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = BuildResetPasswordModel(userManagerMock, unknownEmail, out _, out _);

        // Act
        var actionResult = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(PageRoutes.SiblingResetPasswordConfirmation, redirect.PageName);
        userManagerMock.Verify(um => um.FindByEmailAsync(unknownEmail), Times.Once);
        userManagerMock.Verify(
            um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ResetSucceeds_RedirectsToConfirmation()
    {
        // Arrange
        var resettingEmail = Generated.NewEmailAddress();
        var resettingUser = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId(),
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
        Assert.Equal(PageRoutes.SiblingResetPasswordConfirmation, redirect.PageName);
        userManagerMock.Verify(um => um.FindByEmailAsync(resettingEmail), Times.Once);
        userManagerMock.Verify(um => um.ResetPasswordAsync(resettingUser, resetCode, newPassword), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var resettingEmail = Generated.NewEmailAddress();
        var resetCode = Generated.NewProviderKey();
        var newPassword = Generated.NewPassword();
        var firstError = Generated.NewFailureReason();
        var secondError = Generated.NewFailureReason();
        var foundUser = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId(),
            Email = resettingEmail,
            UserName = Generated.NewUserName()
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

        var model = new ResetPassword(userManagerMock.Object)
        {
            Input = new ResetPassword.InputModel
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

    private static ResetPassword BuildResetPasswordModel(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        string email,
        out string resetCode,
        out string newPassword)
    {
        resetCode = Generated.NewEmailConfirmationToken();
        newPassword = Generated.NewPassword();

        return new ResetPassword(userManagerMock.Object)
        {
            Input = new ResetPassword.InputModel
            {
                Email = email,
                Password = newPassword,
                ConfirmPassword = newPassword,
                Code = resetCode
            }
        };
    }
}
