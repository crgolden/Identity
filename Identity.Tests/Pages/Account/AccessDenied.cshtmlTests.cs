namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Tests for <see cref="AccessDeniedModel"/>.
/// </summary>
[Trait("Category", "Unit")]
public class AccessDeniedModelTests
{
    /// <summary>
    /// Verifies that the <see cref="AccessDeniedModel"/> class is decorated with <see cref="AllowAnonymousAttribute"/>.
    /// Input conditions: reflection-based inspection of the type.
    /// Expected result: the attribute is present on the class.
    /// </summary>
    [Fact]
    public void AccessDeniedModel_Class_HasAllowAnonymousAttribute()
    {
        // Arrange & Act
        var hasAttribute = Attribute.IsDefined(typeof(AccessDeniedModel), typeof(AllowAnonymousAttribute));

        // Assert
        Assert.True(hasAttribute);
    }
}