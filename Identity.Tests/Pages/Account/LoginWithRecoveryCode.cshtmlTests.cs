// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Tests.Pages.Account
{
    /// <summary>
    /// Tests for LoginWithRecoveryCodeModel.OnGetAsync
    /// </summary>
    public class LoginWithRecoveryCodeModelTests
    {
        /// <summary>
        /// Verifies that when a two-factor authentication user exists the handler returns a PageResult and sets ReturnUrl to the provided value.
        /// Input conditions: SignInManager.GetTwoFactorAuthenticationUserAsync returns a non-null IdentityUser; various returnUrl inputs including null, empty, whitespace, long and special-character strings are tested.
        /// Expected result: Method returns PageResult and model.ReturnUrl equals the provided returnUrl.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReturnUrlValues))]
        public async Task OnGetAsync_TwoFactorUserExists_SetsReturnUrlAndReturnsPage(string? returnUrl)
        {
            // Arrange
            var twoFactorUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser" };

            var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
                new Mock<UserManager<IdentityUser<Guid>>>(Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null).Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
                Options.Create(new IdentityOptions()),
                Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

            signInManagerMock
                .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
                .ReturnsAsync(twoFactorUser);

            var userManager = new Mock<UserManager<IdentityUser<Guid>>>(Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null).Object;
            var logger = Mock.Of<ILogger<LoginWithRecoveryCodeModel>>();

            var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManager, logger);

            // Act
            IActionResult result = await model.OnGetAsync(returnUrl);

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(returnUrl, model.ReturnUrl);
        }

        // MemberData providing a variety of string edge cases including null.
        public static IEnumerable<object?[]> ReturnUrlValues()
        {
            yield return new object?[] { null };
            yield return new object?[] { string.Empty };
            yield return new object?[] { " " };
            yield return new object?[] { "/account/manage?return=true" };
            yield return new object?[] { "/path/with/special?param=üñîçødé&x=1" };
            // long string (~2048 chars) to test boundary for very long URLs
            yield return new object?[] { new string('a', 2048) };
        }

        /// <summary>
        /// Verifies that when ModelState is invalid the handler returns PageResult without calling sign-in flows.
        /// Input conditions: ModelState contains an error.
        /// Expected result: PageResult is returned.
        /// </summary>
        [Fact]
        public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
        {
            // Arrange
            var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
            var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
            var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
                userManagerMock.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
                Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
                Mock.Of<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
            var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();

            var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);
            // Make ModelState invalid
            model.ModelState.AddModelError("key", "error");
            model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "irrelevant" };

            // Act
            IActionResult result = await model.OnPostAsync(null);

            // Assert
            Assert.IsType<PageResult>(result);
            // Ensure sign-in manager methods were not invoked
            signInManagerMock.Verify(s => s.GetTwoFactorAuthenticationUserAsync(), Times.Never);
            signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Verifies that when GetTwoFactorAuthenticationUserAsync returns null, an InvalidOperationException is thrown.
        /// Input conditions: valid ModelState, no two-factor user available.
        /// Expected result: InvalidOperationException with specific message.
        /// </summary>
        [Fact]
        public async Task OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
            var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
            var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
                userManagerMock.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
                Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
                Mock.Of<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

            signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);

            var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();

            var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);
            model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "code" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync(null));
            Assert.Equal("Unable to load two-factor authentication user.", ex.Message);

            // Ensure TwoFactorRecoveryCodeSignInAsync was not called
            signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
        }

        public static IEnumerable<object?[]> GetReturnUrlCases()
        {
            // Case: null returnUrl should redirect to Url.Content("~/") which we mock to "/"
            yield return new object?[] { null, "/" };
            // Case: provided returnUrl should be used as-is
            yield return new object?[] { "/some/local/path", "/some/local/path" };
        }

        /// <summary>
        /// Verifies that constructing LoginWithRecoveryCodeModel with all null dependencies does not throw
        /// and that public properties (Input and ReturnUrl) are left at their default (null) values.
        /// Input conditions:
        /// - signInManager: null
        /// - userManager: null
        /// - logger: null
        /// Expected result:
        /// - No exception thrown.
        /// - Instance is created successfully.
        /// - Input and ReturnUrl are null by default.
        /// </summary>
        [Fact]
        public void LoginWithRecoveryCodeModel_Constructor_AllNulls_DoesNotThrowAndDefaults()
        {
            // Arrange
            SignInManager<IdentityUser<Guid>>? signInManager = null;
            UserManager<IdentityUser<Guid>>? userManager = null;
            ILogger<LoginWithRecoveryCodeModel>? logger = null;

            // Act
            var exception = Record.Exception(() => new LoginWithRecoveryCodeModel(signInManager, userManager, logger));
            var model = new LoginWithRecoveryCodeModel(signInManager, userManager, logger);

            // Assert
            Assert.Null(exception);
            Assert.NotNull(model);
            Assert.Null(model.Input);
            Assert.Null(model.ReturnUrl);
        }

        /// <summary>
        /// Verifies that constructing LoginWithRecoveryCodeModel with a provided logger (mocked)
        /// and null managers does not throw and results in default public property values.
        /// Input conditions:
        /// - signInManager: null
        /// - userManager: null
        /// - logger: mocked ILogger<LoginWithRecoveryCodeModel>
        /// Expected result:
        /// - No exception thrown.
        /// - Instance is created successfully.
        /// - Input and ReturnUrl are null by default.
        /// </summary>
        [Fact]
        public void LoginWithRecoveryCodeModel_Constructor_WithLoggerMock_DoesNotThrowAndDefaults()
        {
            // Arrange
            SignInManager<IdentityUser<Guid>>? signInManager = null;
            UserManager<IdentityUser<Guid>>? userManager = null;
            var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();
            ILogger<LoginWithRecoveryCodeModel> logger = loggerMock.Object;

            // Act
            var exception = Record.Exception(() => new LoginWithRecoveryCodeModel(signInManager, userManager, logger));
            var model = new LoginWithRecoveryCodeModel(signInManager, userManager, logger);

            // Assert
            Assert.Null(exception);
            Assert.NotNull(model);
            Assert.Null(model.Input);
            Assert.Null(model.ReturnUrl);
        }

    }
}