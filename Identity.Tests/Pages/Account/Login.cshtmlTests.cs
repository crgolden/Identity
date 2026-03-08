// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Tests.Pages.Account
{
    /// <summary>
    /// Tests for Identity.Pages.Account.LoginModel constructor behavior.
    /// </summary>
    public class LoginModelTests
    {
        /// <summary>
        /// Verifies that the LoginModel constructor does not throw and produces a usable PageModel instance
        /// when signInManager is null and logger is either provided or null.
        /// Inputs:
        ///  - signInManager: null (SignInManager{IdentityUser{Guid}}?).
        ///  - logger: nullable; tested both null and a mocked ILogger instance.
        /// Expected:
        ///  - No exception is thrown.
        ///  - The constructed instance is not null and is assignable to PageModel.
        ///  - Public properties that are not initialized by the constructor (e.g., Input) remain null.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_SignInManagerNull_LoggerOptional_NoThrowAndInitialState(bool provideLogger)
        {
            // Arrange
            SignInManager<IdentityUser<Guid>>? signInManager = null;
            ILogger<LoginModel>? logger = provideLogger
                ? new Mock<ILogger<LoginModel>>().Object
                : null;

            // Act
            LoginModel model = null!;
            Exception? ex = Record.Exception(() => model = new LoginModel(signInManager, logger));

            // Assert
            Assert.Null(ex);
            Assert.NotNull(model);
            Assert.IsAssignableFrom<PageModel>(model);
            // Constructor does not initialize Input; it should remain null.
            Assert.Null(model.Input);
            // ReturnUrl and ExternalLogins are not set by constructor; expect null.
            Assert.Null(model.ReturnUrl);
            Assert.Null(model.ExternalLogins);
        }

        /// <summary>
        /// Verifies that the LoginModel constructor accepts both parameters as null without throwing.
        /// Inputs:
        ///  - signInManager: null
        ///  - logger: null
        /// Expected:
        ///  - No exception is thrown and instance is constructible.
        /// </summary>
        [Fact]
        public void Constructor_BothParametersNull_DoesNotThrow()
        {
            // Arrange
            SignInManager<IdentityUser<Guid>>? signInManager = null;
            ILogger<LoginModel>? logger = null;

            // Act & Assert
            var ex = Record.Exception(() => new LoginModel(signInManager, logger));
            Assert.Null(ex);
        }

        /// <summary>
        /// Verifies that when the model state is invalid and no passkey credential is provided,
        /// the method returns PageResult without calling PasswordSignInAsync.
        /// Input conditions: Input.Passkey is null and ModelState contains an error.
        /// Expected result: returns PageResult and PasswordSignInAsync is not invoked.
        /// </summary>
        [Fact]
        public async Task OnPostAsync_Password_ModelStateInvalid_ReturnsPageWithoutSignInCall()
        {
            // Arrange
            var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
            var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
            var httpContextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
            var signInLoggerMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
            var confirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

            var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
                userManagerMock.Object,
                httpContextAccessorMock.Object,
                claimsFactoryMock.Object,
                optionsMock.Object,
                signInLoggerMock.Object,
                schemesMock.Object,
                confirmationMock.Object)
            { CallBase = false };

            signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(Enumerable.Empty<AuthenticationScheme>());

            var loggerMock = new Mock<ILogger<LoginModel>>();
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

            var model = new LoginModel(signInManagerMock.Object, loggerMock.Object)
            {
                Url = urlHelperMock.Object,
                Input = new LoginModel.InputModel
                {
                    Passkey = null,
                    Email = "user@example.com",
                    Password = "pw",
                    RememberMe = false
                }
            };

            // Make ModelState invalid
            model.ModelState.AddModelError("error", "invalid");

            // Act
            var result = await model.OnPostAsync(returnUrl: null);

            // Assert
            Assert.IsType<PageResult>(result);
            signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

    }
}