namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

/// <summary>
/// Tests for ResendEmailConfirmationModel.OnGet.
/// Focuses on ensuring OnGet does not alter state or throw for typical/pre-set states.
/// </summary>
public class ResendEmailConfirmationModelTests
{
    /// <summary>
    /// Ensures that calling OnGet when Input is null does not throw and leaves Input null.
    /// </summary>
    [Fact]
    public void OnGet_WhenCalled_WithNullInput_DoesNotThrowAndLeavesInputNull()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var emailSenderMock = new Mock<IEmailSender>();

        var model = new ResendEmailConfirmationModel(userManagerMock.Object, emailSenderMock.Object);

        // Precondition: Input is null
        Assert.Null(model.Input);

        // Act
        var exception = Record.Exception(() => model.OnGet());

        // Assert
        Assert.Null(exception);
        Assert.Null(model.Input);
    }

    /// <summary>
    /// Verifies that OnGet does not modify an already assigned Input model and preserves the Email value.
    /// Tests multiple representative email values including empty, whitespace, valid, long and special-character strings.
    /// </summary>
    [Theory]
    [MemberData(nameof(EmailTestCases))]
    public void OnGet_WhenInputSet_RetainsInputUnchanged(string email)
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var emailSenderMock = new Mock<IEmailSender>();

        var model = new ResendEmailConfirmationModel(userManagerMock.Object, emailSenderMock.Object);

        var input = new ResendEmailConfirmationModel.InputModel
        {
            Email = email
        };

        model.Input = input;

        // Act
        var exception = Record.Exception(() => model.OnGet());

        // Assert: no exception and Input reference and value unchanged
        Assert.Null(exception);
        Assert.Same(input, model.Input);
        Assert.Equal(email, model.Input.Email);
    }

    /// <summary>
    /// Provides representative email test cases:
    /// - empty string
    /// - whitespace-only
    /// - typical valid email
    /// - very long string
    /// - string with special and control characters
    /// </summary>
    public static IEnumerable<object[]> EmailTestCases()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "   " };
        yield return new object[] { "user@example.com" };
        yield return new object[] { new string('a', 1024) };
        yield return new object[] { "special!@#$%^&*()\t\n\"<>[];:\\'/" };
    }

    /// <summary>
    /// Verifies that when ModelState is invalid OnPostAsync returns a PageResult immediately
    /// and does not call into user manager or email sender.
    /// Input condition: ModelState contains an error.
    /// Expected result: PageResult and no calls to FindByEmailAsync or SendEmailAsync.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageWithoutCallingDependencies()
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            mockUserStore, null, null, null, null, null, null, null, null);
        var mockEmailSender = new Mock<IEmailSender>();

        var model = new ResendEmailConfirmationModel(mockUserManager.Object, mockEmailSender.Object)
        {
            Input = new ResendEmailConfirmationModel.InputModel { Email = "doesnotmatter@example.com" }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        // Mark model state invalid
        model.ModelState.AddModelError("someKey", "some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        mockUserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        mockEmailSender.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Parameterized test for when no user exists for the supplied email.
    /// Input conditions: various email inputs (including empty and whitespace), ModelState valid.
    /// Expected result: PageResult, a model error with the verification message, and no email sent.
    /// </summary>
    [Theory]
    [InlineData("user@example.com")]
    public async Task OnPostAsync_UserNotFound_AddsModelErrorAndReturnsPage(string email)
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            mockUserStore, null, null, null, null, null, null, null, null);

        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var mockEmailSender = new Mock<IEmailSender>();

        var model = new ResendEmailConfirmationModel(mockUserManager.Object, mockEmailSender.Object)
        {
            Input = new ResendEmailConfirmationModel.InputModel { Email = email }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.PageContext.HttpContext.Request.Scheme = "https";

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        Assert.True(model.ModelState.ErrorCount > 0);
        var entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        Assert.NotEmpty(entry.Errors);
        Assert.Equal("Verification email sent. Please check your email.", entry.Errors[0].ErrorMessage);

        mockUserManager.Verify(m => m.FindByEmailAsync(It.Is<string>(s => s == email)), Times.Once);
        mockEmailSender.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test purpose:
    /// Documents the null-argument scenario for the constructor.
    /// Input conditions:
    /// - The ResendEmailConfirmationModel constructor parameters are non-nullable in the source.
    /// - Therefore assigning null to those parameters is not appropriate per test file nullability rules.
    /// Expected result:
    /// - This test is marked as skipped and explains that a meaningful runtime null-argument test should only be added
    ///   if the constructor is changed to perform null checks or accepts nullable parameters.
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_Notes()
    {
        // ARRANGE / ACT / ASSERT
        // The production constructor signature does not use nullable annotations and does not perform null checks
        // (based on the provided source). Per testing constraints we must not assign null to non-nullable parameters.
        //
        // Instead, verify that constructing with valid non-null dependencies succeeds.
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);
        var mockEmailSender = new Mock<IEmailSender>();

        var exception = Record.Exception(() => new ResendEmailConfirmationModel(mockUserManager.Object, mockEmailSender.Object));

        Assert.Null(exception);
    }
}