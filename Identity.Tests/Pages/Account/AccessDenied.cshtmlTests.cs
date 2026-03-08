// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

#nullable enable
using Identity.Pages.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Tests.Pages.Account
{
    /// <summary>
    /// Tests for <see cref="AccessDeniedModel"/>.
    /// </summary>
    public class AccessDeniedModelTests
    {
        /// <summary>
        /// Verifies that calling OnGet does not throw and the instance remains a PageModel.
        /// Input conditions: a default-constructed <see cref="AccessDeniedModel"/>.
        /// Expected result: no exception is thrown; instance is non-null, of the correct type, and assignable to <see cref="PageModel"/>.
        /// </summary>
        [Fact]
        public void OnGet_WhenInvoked_DoesNotThrowAndModelRemainsPageModel()
        {
            // Arrange
            var model = new AccessDeniedModel();

            // Act
            Exception? ex = Record.Exception(() => model.OnGet());

            // Assert
            Assert.Null(ex);
            Assert.IsType<AccessDeniedModel>(model);
            Assert.IsAssignableFrom<PageModel>(model);
        }

        /// <summary>
        /// Verifies that the <see cref="AccessDeniedModel"/> class is decorated with <see cref="AllowAnonymousAttribute"/>.
        /// Input conditions: reflection-based inspection of the type.
        /// Expected result: the attribute is present on the class.
        /// </summary>
        [Fact]
        public void AccessDeniedModel_Class_HasAllowAnonymousAttribute()
        {
            // Arrange & Act
            bool hasAttribute = Attribute.IsDefined(typeof(AccessDeniedModel), typeof(AllowAnonymousAttribute));

            // Assert
            Assert.True(hasAttribute);
        }
    }
}