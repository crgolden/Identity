#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

/// <summary>
/// Tests for Identity.Pages.Account.ForgotPasswordModel.OnPostAsync
/// </summary>
public class ForgotPasswordModelTests
{
    /// <summary>
    /// Test Purpose:
    /// Verifies that when ModelState is invalid the handler returns PageResult without calling user manager or email sender.
    /// Input Conditions:
    /// - ModelState contains a model error.
    /// Expected Result:
    /// - Method returns PageResult and no calls are made to FindByEmailAsync or SendEmailAsync.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var store = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            store.Object, null, null, null, null, null, null, null, null);

        var emailSenderMock = new Mock<IEmailSender>(MockBehavior.Strict);

        var model = new ForgotPasswordModel(userManagerMock.Object, emailSenderMock.Object);

        // Make ModelState invalid
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.ModelState.AddModelError("Email", "Required");

        model.Input = new ForgotPasswordModel.InputModel { Email = "user@example.com" };

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        // Ensure no calls were made to user manager or email sender
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test Purpose:
    /// Verifies that when the found user is null or email is not confirmed the handler redirects to the confirmation page and does not send email.
    /// Input Conditions:
    /// - ModelState is valid.
    /// - Parameterized:
    ///   * userExists = false -> FindByEmailAsync returns null
    ///   * userExists = true, isConfirmed = false -> FindByEmailAsync returns user and IsEmailConfirmedAsync returns false
    /// Expected Result:
    /// - Method returns RedirectToPageResult to "./ForgotPasswordConfirmation".
    /// - No email is sent.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task OnPostAsync_UserNullOrUnconfirmed_RedirectsToConfirmation_DoesNotSendEmail(bool userExists, bool isConfirmed)
    {
        // Arrange
        var store = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            store.Object, null, null, null, null, null, null, null, null);

        var emailSenderMock = new Mock<IEmailSender>(MockBehavior.Strict);

        var model = new ForgotPasswordModel(userManagerMock.Object, emailSenderMock.Object);

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.Input = new ForgotPasswordModel.InputModel { Email = "user@example.com" };

        if (!userExists)
        {
            userManagerMock.Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == model.Input.Email)))
                .ReturnsAsync((IdentityUser<Guid>?)null)
                .Verifiable();
        }
        else
        {
            var user = new IdentityUser<Guid> { UserName = "u", Email = model.Input.Email };
            userManagerMock.Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == model.Input.Email)))
                .ReturnsAsync(user)
                .Verifiable();

            userManagerMock.Setup(um => um.IsEmailConfirmedAsync(It.Is<IdentityUser<Guid>>(u => u == user)))
                .ReturnsAsync(isConfirmed)
                .Verifiable();
        }

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ForgotPasswordConfirmation", redirect.PageName);

        // Email should not be sent in these cases
        emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        userManagerMock.Verify();
    }

    /// <summary>
    /// Test constructor behavior when dependencies may be null or present.
    /// This parameterized test covers combinations where the UserManager and IEmailSender
    /// are either provided or null. The constructor in the source simply assigns fields
    /// without validation, so construction should succeed without throwing in all cases.
    /// Input conditions:
    /// - userManagerNull: when true, pass null for UserManager; otherwise construct a minimal UserManager.
    /// - emailSenderNull: when true, pass null for IEmailSender; otherwise pass a mocked IEmailSender.
    /// Expected result: No exception is thrown, the model instance is created, it derives from PageModel,
    /// and the Input property is null by default.
    /// </summary>
    [Theory]
    [MemberData(nameof(ConstructorNullCombinations))]
    public void ForgotPasswordModel_Constructor_DependencyNullAllowed_ShouldCreateInstance(bool userManagerNull, bool emailSenderNull)
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        IEmailSender? emailSender = null;

        if (!userManagerNull)
        {
            // Provide a minimal IUserStore mock required by UserManager constructor.
            var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
            // It's acceptable to pass null for many of the optional services in the UserManager ctor.
            userManager = new UserManager<IdentityUser<Guid>>(
                storeMock.Object,
                optionsAccessor: null,
                passwordHasher: null,
                userValidators: null,
                passwordValidators: null,
                keyNormalizer: null,
                errors: null,
                services: null,
                logger: null);
        }

        if (!emailSenderNull)
        {
            var emailSenderMock = new Mock<IEmailSender>();
            emailSender = emailSenderMock.Object;
        }

        // Act
        Exception? ex = Record.Exception(() => new ForgotPasswordModel(userManager, emailSender));
        var model = new ForgotPasswordModel(userManager, emailSender);

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<ForgotPasswordModel>(model);
        Assert.IsAssignableFrom<PageModel>(model);
        // By default Input should be null (not initialized in constructor)
        Assert.Null(model.Input);
    }

    public static IEnumerable<object[]> ConstructorNullCombinations()
    {
        // userManagerNull, emailSenderNull
        yield return new object[] { false, false }; // both provided
        yield return new object[] { true, false };  // userManager null
        yield return new object[] { false, true };  // emailSender null
        yield return new object[] { true, true };   // both null
    }
}